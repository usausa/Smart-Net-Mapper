namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection.Emit;

    internal class EmitMapperBuilder
    {
        private readonly ILGenerator ilGenerator;

        // Info

        private readonly MapperCreateContext context;

        private readonly EmitHolderInfo holder;

        private readonly bool isFunction;

        private readonly bool hasParameter;

        // Work

        private LocalBuilder? sourceLocal;
        private LocalBuilder? destinationLocal;
        private LocalBuilder? contextLocal;

        private readonly Dictionary<Type, LocalBuilder> workLocals = new();

        public EmitMapperBuilder(
            ILGenerator ilGenerator,
            MapperCreateContext context,
            EmitHolderInfo holder,
            bool isFunction,
            bool hasParameter)
        {
            this.context = context;
            this.ilGenerator = ilGenerator;
            this.holder = holder;
            this.isFunction = isFunction;
            this.hasParameter = hasParameter;
        }

        //--------------------------------------------------------------------------------
        // Build
        //--------------------------------------------------------------------------------

        public void Build()
        {
            DeclareVariables();
            if (context.DelegateSourceType.IsClass)
            {
                EmitGuardClass();
            }
            else if (context.DelegateSourceType.IsNullableType())
            {
                EmitGuardNullable();
            }
            EmitContext();
            if (isFunction)
            {
                EmitNewDestination();
            }
            EmitMapActions(true);
            EmitMemberMapping();
            EmitMapActions(false);
            EmitReturn();
        }

        //--------------------------------------------------------------------------------
        // Prepare
        //--------------------------------------------------------------------------------

        private void DeclareVariables()
        {
            // Destination
            if (isFunction && (holder.DestinationParameterRequired || !context.MapDestinationType.IsClass))
            {
                destinationLocal = ilGenerator.DeclareLocal(context.MapDestinationType);
            }

            // Source
            if (context.DelegateSourceType.IsNullableType())
            {
                sourceLocal = ilGenerator.DeclareLocal(Nullable.GetUnderlyingType(context.DelegateSourceType)!);
            }

            // Context
            if (holder.ContextRequired)
            {
                contextLocal = ilGenerator.DeclareLocal(typeof(ResolutionContext));
            }
        }

        private void EmitGuardClass()
        {
            // Class
            var hasValueLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Brtrue_S, hasValueLabel);
            if (isFunction)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator.MarkLabel(hasValueLabel);
        }

        private void EmitGuardNullable()
        {
            // Nullable
            var hasValueLabel = ilGenerator.DefineLabel();

            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Call, context.DelegateSourceType.GetProperty("HasValue")!.GetMethod!);
            ilGenerator.Emit(OpCodes.Brtrue_S, hasValueLabel);
            if (isFunction)
            {
                ilGenerator.EmitLdloca(destinationLocal!);
                ilGenerator.Emit(OpCodes.Initobj, context.DelegateDestinationType);
                ilGenerator.EmitLdloc(destinationLocal!);
            }
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator.MarkLabel(hasValueLabel);

            // Source value
            ilGenerator.Emit(OpCodes.Ldarga_S, 1);
            ilGenerator.Emit(OpCodes.Call, context.DelegateSourceType.GetProperty("Value")!.GetMethod!);
            ilGenerator.EmitStloc(sourceLocal!);
        }

        private void EmitContext()
        {
            // Context
            if (contextLocal is not null)
            {
                ilGenerator.Emit(hasParameter ? (isFunction ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3) : OpCodes.Ldnull);
                EmitLoadField(holder.GetMapperField());
                ilGenerator.Emit(OpCodes.Newobj, typeof(ResolutionContext).GetConstructor(new[] { typeof(object), typeof(INestedMapper) })!);
                ilGenerator.EmitStloc(contextLocal);
            }
        }

        //--------------------------------------------------------------------------------
        // Constructor
        //--------------------------------------------------------------------------------

        private void EmitNewDestination()
        {
            if (context.IsFactoryUseServiceProvider)
            {
                // IServiceProvider
                var field = holder.GetFactoryField();
                EmitLoadField(field);
                ilGenerator.Emit(OpCodes.Ldtoken, context.MapDestinationType);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
                var method = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService), new[] { typeof(Type) })!;
                ilGenerator.EmitCallMethod(method);
                ilGenerator.EmitTypeConversion(context.MapDestinationType);

                if (destinationLocal is not null)
                {
                    ilGenerator.EmitStloc(destinationLocal);
                }
            }
            else if (context.Factory is not null)
            {
                var field = holder.GetFactoryField();
                switch (context.Factory.Type)
                {
                    case FactoryType.FuncDestination:
                        EmitLoadField(field);
                        EmitCallFieldMethod(field, "Invoke");
                        break;
                    case FactoryType.FuncSourceDestination:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitCallFieldMethod(field, "Invoke");
                        break;
                    case FactoryType.FuncSourceContextDestination:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitStackContextArgument();
                        EmitCallFieldMethod(field, "Invoke");
                        break;
                    case FactoryType.Interface:
                    case FactoryType.InterfaceType:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitStackContextArgument();
                        EmitCallFieldMethod(field, "Create");
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported factory. type=[{context.Factory.Type}]");
                }

                if (destinationLocal is not null)
                {
                    ilGenerator.EmitStloc(destinationLocal);
                }
            }
            else
            {
                if (context.MapDestinationType.IsClass)
                {
                    // Default constructor
                    var ctor = context.MapDestinationType.GetConstructor(Type.EmptyTypes);
                    if (ctor is null)
                    {
                        throw new InvalidOperationException($"Type has not default constructor. type=[{context.MapDestinationType}]");
                    }

                    ilGenerator.Emit(OpCodes.Newobj, ctor);

                    if (destinationLocal is not null)
                    {
                        ilGenerator.EmitStloc(destinationLocal);
                    }
                }
                else
                {
                    // Struct
                    ilGenerator.EmitLdloca(destinationLocal!);
                    ilGenerator.Emit(OpCodes.Initobj, context.MapDestinationType);
                }
            }
        }

        //--------------------------------------------------------------------------------
        // Action
        //--------------------------------------------------------------------------------

        private void EmitMapActions(bool before)
        {
            var actions = before ? context.BeforeMaps : context.AfterMaps;
            for (var i = 0; i < actions.Count; i++)
            {
                var field = before ? holder.GetBeforeMapField(i) : holder.GetAfterMapField(i);
                switch (actions[i].Type)
                {
                    case ActionType.Action:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitStackDestinationArgument();
                        EmitCallFieldMethod(field, "Invoke");
                        break;
                    case ActionType.ActionContext:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitStackDestinationArgument();
                        EmitStackContextArgument();
                        EmitCallFieldMethod(field, "Invoke");
                        break;
                    case ActionType.Interface:
                    case ActionType.InterfaceType:
                        EmitLoadField(field);
                        EmitStackSourceArgument();
                        EmitStackDestinationArgument();
                        EmitStackContextArgument();
                        EmitCallFieldMethod(field, "Process");
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported action map. type=[{field.FieldType}]");
                }
            }
        }

        //--------------------------------------------------------------------------------
        // Mapping
        //--------------------------------------------------------------------------------

        private void EmitMemberMapping()
        {
            foreach (var member in context.Members)
            {
                var nextLabel = (member.Condition is not null) ? ilGenerator.DefineLabel() : default;

                // Condition
                if (member.Condition is not null)
                {
                    EmitEvalCondition(member);

                    ilGenerator.Emit(OpCodes.Brfalse, nextLabel);
                }

                // Destination for set
                EmitStackDestinationCall();

                // Value expression
                if (member.IsConst)
                {
                    // Const
                    EmitLoadField(holder.GetConstValueField(member.No));
                }
                else if (member.IsNested)
                {
                    // Nested
                    var field = hasParameter ? holder.GetParameterNestedMapperField(member.No) : holder.GetNestedMapperField(member.No);
                    EmitLoadField(field);
                    EmitStackSourceMember(member);
                    EmitStackParameter();
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                }
                else
                {
                    // Prepare converter
                    if (member.Converter is not null)
                    {
                        EmitLoadField(holder.GetConverterField(member.No));
                    }

                    // Stack source
                    EmitStackSourceMember(member);
                    var stackedType = member.MapFrom!.MemberType;

                    // Invoke converter
                    if (member.Converter is not null)
                    {
                        if (Nullable.GetUnderlyingType(member.Converter.SourceType) == stackedType)
                        {
                            ilGenerator.Emit(OpCodes.Newobj, member.Converter.SourceType.GetConstructor(new[] { stackedType })!);
                        }

                        EmitInvokeConverter(member);

                        stackedType = member.Converter.DestinationType;
                    }

                    if (member.IsNullIf && (stackedType.IsClass || stackedType.IsNullableType()))
                    {
                        // NullIf
                        if (stackedType.IsClass)
                        {
                            EmitNullIfClass(member, stackedType);
                        }
                        else
                        {
                            EmitNullIfNullable(member, stackedType);
                        }
                    }
                    else
                    {
                        // Convert
                        if (stackedType.IsClass)
                        {
                            // Class
                            if (!member.Property.PropertyType.IsAssignableFrom(stackedType))
                            {
                                EmitConvertClass(member, stackedType);
                            }
                        }
                        else if (stackedType.IsNullableType())
                        {
                            // Nullable
                            if (member.Property.PropertyType != stackedType)
                            {
                                EmitConvertNullable(member, stackedType);
                            }
                        }
                        else
                        {
                            // ValueType
                            if (member.Property.PropertyType != stackedType)
                            {
                                EmitConvertValueType(member, stackedType);
                            }
                        }
                    }
                }

                // Set
                ilGenerator.EmitCallMethod(member.Property.SetMethod!);

                if (member.Condition is not null)
                {
                    // Condition next:
                    ilGenerator.MarkLabel(nextLabel);
                }
            }
        }

        //--------------------------------------------------------------------------------
        // Invoke
        //--------------------------------------------------------------------------------

        private void EmitEvalCondition(MemberMapping member)
        {
            var field = holder.GetConditionField(member.No);
            switch (member.Condition!.Type)
            {
                case ConditionType.FuncSource:
                    EmitLoadField(field);
                    EmitStackSourceArgument();
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.FuncSourceContext:
                    EmitLoadField(field);
                    EmitStackSourceArgument();
                    EmitStackContextArgument();
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.FuncSourceDestinationContext:
                    EmitLoadField(field);
                    EmitStackSourceArgument();
                    EmitStackDestinationArgument();
                    EmitStackContextArgument();
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.Interface:
                case ConditionType.InterfaceType:
                    EmitLoadField(field);
                    EmitStackSourceArgument();
                    EmitStackDestinationArgument();
                    EmitStackContextArgument();
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Eval")!);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported condition type. type=[{member.Condition.Type}]");
            }
        }

        private void EmitInvokeConverter(MemberMapping member)
        {
            var field = holder.GetConverterField(member.No);
            switch (member.Converter!.Type)
            {
                case ConverterType.FuncSource:
                    EmitCallFieldMethod(field, "Invoke");
                    break;
                case ConverterType.FuncSourceContext:
                    EmitStackContextArgument();
                    EmitCallFieldMethod(field, "Invoke");
                    break;
                case ConverterType.Interface:
                case ConverterType.InterfaceType:
                    EmitStackContextArgument();
                    EmitCallFieldMethod(field, "Convert");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported converter. type=[{member.Converter.Type}]");
            }
        }

        private void EmitStackSourceMember(MemberMapping member)
        {
            switch (member.MapFrom!.Type)
            {
                case FromType.Properties:
                    EmitStackSourceCall();
                    foreach (var pi in (PropertyInfo[])member.MapFrom.Value)
                    {
                        ilGenerator.EmitCallMethod(pi.GetMethod!);
                    }

                    break;
                case FromType.LazyFunc:
                    var lazyFuncField = holder.GetProviderField(member.No);
                    EmitLoadField(lazyFuncField);
                    EmitStackSourceArgument();
                    EmitCallFieldMethod(lazyFuncField, "Invoke");
                    break;
                case FromType.Func:
                    var funcField = holder.GetProviderField(member.No);
                    EmitLoadField(funcField);
                    EmitStackSourceArgument();
                    EmitStackDestinationArgument();
                    EmitCallFieldMethod(funcField, "Invoke");
                    break;
                case FromType.FuncContext:
                    var funcContextField = holder.GetProviderField(member.No);
                    EmitLoadField(funcContextField);
                    EmitStackSourceArgument();
                    EmitStackDestinationArgument();
                    EmitStackContextArgument();
                    EmitCallFieldMethod(funcContextField, "Invoke");
                    break;
                case FromType.Interface:
                case FromType.InterfaceType:
                    var interfaceField = holder.GetProviderField(member.No);
                    EmitLoadField(interfaceField);
                    EmitStackSourceArgument();
                    EmitStackDestinationArgument();
                    EmitStackContextArgument();
                    EmitCallFieldMethod(interfaceField, "Provide");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported map from type. type=[{member.MapFrom!.Type}]");
            }
        }

        //--------------------------------------------------------------------------------
        // NullIf
        //--------------------------------------------------------------------------------

        private void EmitNullIfClass(MemberMapping member, Type stackedType)
        {
            // Class
            var convert = !member.Property.PropertyType.IsAssignableFrom(stackedType);
            var setLabel = ilGenerator.DefineLabel();
            var convertLabel = convert ? ilGenerator.DefineLabel() : default;

            // Branch
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Brtrue_S, convert ? convertLabel : setLabel);

            // Null if
            ilGenerator.Emit(OpCodes.Pop);
            EmitLoadField(holder.GetNullIfValueField(member.No));

            if (convert)
            {
                ilGenerator.Emit(OpCodes.Br_S, setLabel);

                // Convert
                ilGenerator.MarkLabel(convertLabel);

                // Convert
                if (!EmitConvertOperationForClass(ilGenerator, stackedType, member.Property.PropertyType))
                {
                    throw new InvalidOperationException($"Can not convert to property. property=[{member.Property.PropertyType.Name}], type=[{stackedType}]");
                }
            }

            ilGenerator.MarkLabel(setLabel);
        }

        private void EmitNullIfNullable(MemberMapping member, Type stackedType)
        {
            // Nullable
            var setLabel = ilGenerator.DefineLabel();
            var reloadLabel = ilGenerator.DefineLabel();
            var temporaryLocal = ResolveWorkLocal(stackedType);

            ilGenerator.EmitStloc(temporaryLocal);

            // Branch
            ilGenerator.EmitLdloca(temporaryLocal);
            ilGenerator.Emit(OpCodes.Call, member.Property.PropertyType.GetProperty("HasValue")!.GetMethod!);
            ilGenerator.Emit(OpCodes.Brtrue_S, reloadLabel);

            // Null if
            EmitLoadField(holder.GetNullIfValueField(member.No));
            ilGenerator.Emit(OpCodes.Br_S, setLabel);

            // Non null
            ilGenerator.MarkLabel(reloadLabel);

            // Convert
            if (member.Property.PropertyType != stackedType)
            {
                if (!EmitConvertOperationForNullable(ilGenerator, stackedType, member.Property.PropertyType, temporaryLocal))
                {
                    ilGenerator.EmitLdloca(temporaryLocal);
                    ilGenerator.Emit(OpCodes.Call, stackedType.GetProperty("Value")!.GetMethod!);

                    var underlyingType = Nullable.GetUnderlyingType(stackedType)!;
                    if (!EmitConvertOperationForNullableValue(ilGenerator, underlyingType, member.Property.PropertyType) &&
                        !EmitConvertPrimitive(ilGenerator, underlyingType, member.Property.PropertyType))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            else
            {
                ilGenerator.EmitLdloc(temporaryLocal);
            }

            ilGenerator.MarkLabel(setLabel);
        }

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        private void EmitConvertClass(MemberMapping member, Type stackedType)
        {
            // Class
            var setLabel = ilGenerator.DefineLabel();
            var convertLabel = ilGenerator.DefineLabel();

            // Branch
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Brtrue_S, convertLabel);

            // Default
            ilGenerator.EmitStackDefaultValue(member.Property.PropertyType);
            ilGenerator.Emit(OpCodes.Br_S, setLabel);

            // Convert
            ilGenerator.MarkLabel(convertLabel);

            // Convert
            if (!EmitConvertOperationForClass(ilGenerator, stackedType, member.Property.PropertyType))
            {
                throw new InvalidOperationException($"Can not convert to property. property=[{member.Property.PropertyType.Name}], type=[{stackedType}]");
            }

            ilGenerator.MarkLabel(setLabel);
        }

        private void EmitConvertNullable(MemberMapping member, Type stackedType)
        {
            // Nullable
            var setLabel = ilGenerator.DefineLabel();
            var convertLabel = ilGenerator.DefineLabel();
            var temporaryLocal = ResolveWorkLocal(stackedType);

            ilGenerator.EmitStloc(temporaryLocal);

            // Branch
            ilGenerator.EmitLdloca(temporaryLocal);
            ilGenerator.Emit(OpCodes.Call, stackedType.GetProperty("HasValue")!.GetMethod!);
            ilGenerator.Emit(OpCodes.Brtrue_S, convertLabel);

            // Default
            ilGenerator.EmitStackDefaultValue(member.Property.PropertyType);
            ilGenerator.Emit(OpCodes.Br_S, setLabel);

            // Non null
            ilGenerator.MarkLabel(convertLabel);

            // Convert
            if (!EmitConvertOperationForNullable(ilGenerator, stackedType, member.Property.PropertyType, temporaryLocal))
            {
                ilGenerator.EmitLdloca(temporaryLocal);
                ilGenerator.Emit(OpCodes.Call, stackedType.GetProperty("Value")!.GetMethod!);

                var underlyingType = Nullable.GetUnderlyingType(stackedType)!;
                if (!EmitConvertOperationForNullableValue(ilGenerator, underlyingType, member.Property.PropertyType) &&
                    !EmitConvertPrimitive(ilGenerator, underlyingType, member.Property.PropertyType))
                {
                    throw new InvalidOperationException($"Can not convert to property. property=[{member.Property.PropertyType.Name}], type=[{stackedType}]");
                }
            }

            ilGenerator.MarkLabel(setLabel);
        }

        private void EmitConvertValueType(MemberMapping member, Type stackedType)
        {
            // Convert
            if (!EmitConvertOperationValueType(ilGenerator, stackedType, member.Property.PropertyType) &&
                !EmitConvertPrimitive(ilGenerator, stackedType, member.Property.PropertyType))
            {
                throw new InvalidOperationException($"Can not convert to property. property=[{member.Property.PropertyType.Name}], type=[{stackedType}]");
            }
        }

        //--------------------------------------------------------------------------------
        // Convert helper
        //--------------------------------------------------------------------------------

        private static bool EmitConvertOperationForClass(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationForNullable(ILGenerator ilGenerator, Type sourceType, Type destinationType, LocalBuilder local)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, false);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Ldloc, local);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, false);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Ldloc, local);
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationForNullableValue(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationValueType(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            var nullableSourceType = typeof(Nullable<>).MakeGenericType(sourceType);
            opMethod = FindConversionOperator(nullableSourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, nullableSourceType.GetConstructor(new[] { sourceType })!);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(nullableSourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Newobj, nullableSourceType.GetConstructor(new[] { sourceType })!);
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertPrimitive(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Try primitive covert
            var baseSourceType = sourceType.IsEnum ? Enum.GetUnderlyingType(sourceType) : sourceType;
            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            var baseDestinationType = underlyingDestinationType ?? destinationType;
            baseDestinationType = baseDestinationType.IsEnum ? Enum.GetUnderlyingType(baseDestinationType) : baseDestinationType;

            if ((baseDestinationType != baseSourceType) &&
                !ilGenerator.EmitPrimitiveConvert(baseSourceType, baseDestinationType))
            {
                return false;
            }

            // If destination is nullable, convert to nullable
            if (underlyingDestinationType is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
            }

            return true;
        }

        private static MethodInfo? FindConversionOperator(Type sourceType, Type destinationType, bool useSourceMethod)
        {
            if (useSourceMethod)
            {
                var sourceTypeMethod = sourceType.GetMethods().FirstOrDefault(mi =>
                    mi.IsPublic && mi.IsStatic && mi.Name == "op_Implicit" && mi.ReturnType == destinationType);
                if (sourceTypeMethod is not null)
                {
                    return sourceTypeMethod;
                }
            }

            var method = destinationType.GetMethods().FirstOrDefault(mi =>
                mi.IsPublic && mi.IsStatic && mi.Name == "op_Implicit" && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == sourceType);
            if (method is not null)
            {
                return method;
            }

            if (useSourceMethod)
            {
                var sourceTypeMethod = sourceType.GetMethods().FirstOrDefault(mi =>
                    mi.IsPublic && mi.IsStatic && mi.Name == "op_Explicit" && mi.ReturnType == destinationType);
                if (sourceTypeMethod is not null)
                {
                    return sourceTypeMethod;
                }
            }

            return destinationType.GetMethods().FirstOrDefault(mi =>
                mi.IsPublic && mi.IsStatic && mi.Name == "op_Explicit" && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == sourceType);
        }

        //--------------------------------------------------------------------------------
        // Return
        //--------------------------------------------------------------------------------

        private void EmitReturn()
        {
            if (isFunction)
            {
                if (destinationLocal is not null)
                {
                    ilGenerator.EmitLdloc(destinationLocal);
                }

                if (Nullable.GetUnderlyingType(context.DelegateDestinationType) == context.MapDestinationType)
                {
                    ilGenerator.Emit(OpCodes.Newobj, context.DelegateDestinationType.GetConstructor(new[] { context.MapDestinationType })!);
                }
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        private LocalBuilder ResolveWorkLocal(Type type)
        {
            if (!workLocals.TryGetValue(type, out var local))
            {
                local = ilGenerator.DeclareLocal(type);
                workLocals[type] = local;
            }

            return local;
        }

        private void EmitLoadField(FieldInfo field)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);
        }

        private void EmitCallFieldMethod(FieldInfo field, string name)
        {
            ilGenerator.EmitCallMethod(field.FieldType.GetMethod(name)!);
        }

        private void EmitStackContextArgument()
        {
            ilGenerator.EmitLdloc(contextLocal!);
        }

        private void EmitStackSourceArgument()
        {
            ilGenerator.Emit(OpCodes.Ldarg_1);
        }

        private void EmitStackDestinationArgument()
        {
            if (isFunction)
            {
                if (destinationLocal is not null)
                {
                    ilGenerator.EmitLdloc(destinationLocal);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Dup);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_2);
            }
        }

        private void EmitStackSourceCall()
        {
            if (context.DelegateDestinationType.IsClass)
            {
                ilGenerator.Emit(OpCodes.Ldarg_1);
            }
            else
            {
                if (sourceLocal is not null)
                {
                    ilGenerator.EmitLdloca(sourceLocal);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarga_S, 1);
                }
            }
        }

        private void EmitStackDestinationCall()
        {
            if (isFunction)
            {
                if (context.MapDestinationType.IsClass)
                {
                    if (destinationLocal is not null)
                    {
                        ilGenerator.EmitLdloc(destinationLocal);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                    }
                }
                else
                {
                    ilGenerator.EmitLdloca(destinationLocal!);
                }
            }
            else
            {
                if (context.DelegateDestinationType.IsClass)
                {
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarga_S, 2);
                }
            }
        }

        private void EmitStackParameter()
        {
            if (hasParameter)
            {
                ilGenerator.Emit(isFunction ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3);
            }
        }
    }
}
