namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
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

        private readonly Dictionary<Type, LocalBuilder> temporaryLocals = new();

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
            EmitGuard();
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
            // TODO re check
            // Destination
            if (isFunction && (holder.HasDestinationParameter || !context.MapDestinationType.IsClass))
            {
                destinationLocal = ilGenerator.DeclareLocal(context.MapDestinationType);
            }

            // Source
            if (context.DelegateSourceType.IsNullableType())
            {
                sourceLocal = ilGenerator.DeclareLocal(Nullable.GetUnderlyingType(context.DelegateSourceType)!);
            }

            // Context
            if (holder.HasContext)
            {
                contextLocal = ilGenerator.DeclareLocal(typeof(ResolutionContext));
            }

            // Temporary
            foreach (var member in context.Members)
            {
                if (member.IsNullIf && !member.Property.PropertyType.IsClass && !temporaryLocals.ContainsKey(member.Property.PropertyType))
                {
                    temporaryLocals[member.Property.PropertyType] = ilGenerator.DeclareLocal(member.Property.PropertyType);
                }

                if (member.IsNested && !temporaryLocals.ContainsKey(member.MapFrom!.MemberType))
                {
                    temporaryLocals[member.MapFrom.MemberType] = ilGenerator.DeclareLocal(member.MapFrom.MemberType);
                }
            }
        }

        private void EmitGuard()
        {
            if (context.DelegateSourceType.IsClass)
            {
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
            else if (context.DelegateSourceType.IsNullableType())
            {
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
                        throw new InvalidOperationException($"Unsupported factory. type=[{field.FieldType}]");
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
                        throw new InvalidOperationException($"Unsupported action map. type=[{field.FieldType}]");
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
                    EmitStackSourceMember(member);

                    // TODO stack型の保持＆移行書き換え？
                    var stackedType = member.MapFrom!.MemberType;

                    // TODO converter適用後にIsNullIf
                    if (member.Converter is not null)
                    {
                        // TODO dが非nullは許可する！
                        // sがnull入る
                        //   nullならデフォルト
                        // else
                        //   Convert
                        //   convertとdが違う?(nullable)
                        //     型変換
                    }
                    else if (member.IsNullIf)
                    {
                        var convert = !member.Property.PropertyType.IsAssignableFrom(stackedType);
                        var setLabel = ilGenerator.DefineLabel();

                        if (member.Property.PropertyType.IsClass)
                        {
                            var convertLabel = convert ? ilGenerator.DefineLabel() : default;

                            // Branch
                            ilGenerator.Emit(OpCodes.Dup);
                            ilGenerator.Emit(OpCodes.Brtrue_S, convert ? convertLabel : setLabel);

                            // Null if
                            ilGenerator.Emit(OpCodes.Pop);
                            EmitLoadField(holder.GetNullIfValueField(member.No));
                            if (convert)
                            {
                                // TODO S?
                                ilGenerator.Emit(OpCodes.Br_S, setLabel);

                                // Convert
                                ilGenerator.MarkLabel(convertLabel);

                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            var reloadLabel = ilGenerator.DefineLabel();
                            var temporaryLocal = temporaryLocals[member.Property.PropertyType];

                            ilGenerator.EmitStloc(temporaryLocal);

                            // Branch
                            ilGenerator.EmitLdloca(temporaryLocal);
                            ilGenerator.Emit(OpCodes.Call, member.Property.PropertyType.GetProperty("HasValue")!.GetMethod!);
                            ilGenerator.Emit(OpCodes.Brtrue_S, reloadLabel);

                            // Null if
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldfld, holder.GetNullIfValueField(member.No));
                            ilGenerator.Emit(OpCodes.Br_S, setLabel);

                            // Non null
                            ilGenerator.MarkLabel(reloadLabel);
                            ilGenerator.EmitLdloc(temporaryLocal);

                            if (convert)
                            {
                                throw new NotImplementedException();
                            }
                        }

                        ilGenerator.MarkLabel(setLabel);
                    }
                    else if (!member.Property.PropertyType.IsAssignableFrom(stackedType))
                    {
                        // TODO convert 独立？
                        throw new NotImplementedException();
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
        // Helper
        //--------------------------------------------------------------------------------

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

        private void EmitReturn()
        {
            if (isFunction)
            {
                if (destinationLocal is not null)
                {
                    ilGenerator.EmitLdloc(destinationLocal);
                }

                if (context.DelegateDestinationType != context.MapDestinationType)
                {
                    if (Nullable.GetUnderlyingType(context.DelegateDestinationType) == context.MapDestinationType)
                    {
                        var nullableCtor = context.DelegateDestinationType.GetConstructor(new[] { context.MapDestinationType });
                        ilGenerator.Emit(OpCodes.Newobj, nullableCtor!);
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported conversion. type=[{context.MapDestinationType}] to type=[{context.DelegateDestinationType}]");
                    }
                }
            }

            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}
