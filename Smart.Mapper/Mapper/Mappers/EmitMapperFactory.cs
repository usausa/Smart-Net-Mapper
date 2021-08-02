namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Linq;
    using Smart.Reflection.Emit;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
    internal class EmitMapperFactory : IMapperFactory
    {
        private readonly IServiceProvider serviceProvider;

        private int typeNo;

        private AssemblyBuilder? assemblyBuilder;

        private ModuleBuilder? moduleBuilder;

        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder is null)
                {
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("SmartMapperAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "SmartMapperModule");
                }

                return moduleBuilder;
            }
        }

        public EmitMapperFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public object Create(MapperCreateContext context)
        {
            // Mapper
            var mapper = CreateMapperInfo(context);

            // Holder
            var holder = CreateHolder(context);

            mapper.GetType().GetField("MapAction")!.SetValue(mapper, CreateMapAction(context, holder));
            mapper.GetType().GetField("MapFunc")!.SetValue(mapper, CreateMapFunc(context, holder));
            mapper.GetType().GetField("ParameterMapAction")!.SetValue(mapper, CreateParameterMapAction(context, holder));
            mapper.GetType().GetField("ParameterMapFunc")!.SetValue(mapper, CreateParameterMapFunc(context, holder));

            return mapper;
        }

        //--------------------------------------------------------------------------------
        // Mapper
        //--------------------------------------------------------------------------------

        private static object CreateMapperInfo(MapperCreateContext context)
        {
            var type = typeof(MapperInfo<,>).MakeGenericType(context.SourceType, context.DestinationType);
            return Activator.CreateInstance(type)!;
        }

        //--------------------------------------------------------------------------------
        // Holder
        //--------------------------------------------------------------------------------

        private HolderInfo CreateHolder(MapperCreateContext context)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            var hasDestinationParameter = IsDestinationParameterRequired(context);
            var hasContext = IsContextRequired(context);

            // Nested mapper
            if (hasContext)
            {
                typeBuilder.DefineField("mapper", typeof(INestedMapper), FieldAttributes.Public);
            }

            // TODO Nested

            // Factory
            var factory = ResolveFactory(context.Factory);
            if (context.IsFactoryUseServiceProvider)
            {
                typeBuilder.DefineField("factory", typeof(IServiceProvider), FieldAttributes.Public);
            }
            else if (factory is not null)
            {
                typeBuilder.DefineField("factory", factory.GetType(), FieldAttributes.Public);
            }

            // BeforeMap
            var beforeMaps = context.BeforeMaps.Select(ResolveAction).ToList();
            for (var i = 0; i < beforeMaps.Count; i++)
            {
                typeBuilder.DefineField($"beforeMap{i}", beforeMaps[i].GetType(), FieldAttributes.Public);
            }

            // AfterMap
            var afterMaps = context.AfterMaps.Select(ResolveAction).ToList();
            for (var i = 0; i < afterMaps.Count; i++)
            {
                typeBuilder.DefineField($"afterMap{i}", afterMaps[i].GetType(), FieldAttributes.Public);
            }

            // Condition
            var conditions = context.Members.Where(x => x.Condition is not null)
                .Select(x => new { Member = x, Condition = ResolveCondition(x.Condition!) })
                .ToList();
            foreach (var condition in conditions)
            {
                typeBuilder.DefineField($"condition{condition.Member.No}", condition.Condition.GetType(), FieldAttributes.Public);
            }

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                typeBuilder.DefineField($"constValue{member.No}", member.Property.PropertyType, FieldAttributes.Public);
            }

            // Provider
            var providers = context.Members.Where(x => x.MapFrom is not null)
                .Select(x => new { Member = x, Provider = ResolveProvider(x.MapFrom!) })
                .Where(x => x.Provider is not null)
                .ToList();
            foreach (var provider in providers)
            {
                typeBuilder.DefineField($"provider{provider.Member.No}", provider.Provider!.GetType(), FieldAttributes.Public);
            }

            // Converter
            var converters = context.Members.Where(x => x.Converter is not null)
                .Select(x => new { Member = x, Converter = ResolveConverter(x.Converter!) })
                .ToList();
            foreach (var converter in converters)
            {
                typeBuilder.DefineField($"converter{converter.Member.No}", converter.Converter.GetType(), FieldAttributes.Public);
            }

            // NullIf
            foreach (var member in context.Members.Where(x => x.IsNullIf))
            {
                typeBuilder.DefineField($"nullIfValue{member.No}", member.Property.PropertyType, FieldAttributes.Public);
            }

            // Create holder
            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // Nested mapper
            if (hasContext)
            {
                GetMapperField(holderType).SetValue(holder, context.NexMapper);
            }

            // TODO Nested

            // Factory
            if (context.IsFactoryUseServiceProvider)
            {
                GetFactoryField(holderType).SetValue(holder, serviceProvider);
            }
            else if (factory is not null)
            {
                GetFactoryField(holderType).SetValue(holder, factory);
            }

            // BeforeMap
            for (var i = 0; i < beforeMaps.Count; i++)
            {
                GetBeforeMapField(holderType, i).SetValue(holder, beforeMaps[i]);
            }

            // AfterMap
            for (var i = 0; i < afterMaps.Count; i++)
            {
                GetAfterMapField(holderType, i).SetValue(holder, afterMaps[i]);
            }

            // Condition
            foreach (var condition in conditions)
            {
                GetConditionField(holderType, condition.Member.No).SetValue(holder, condition.Condition);
            }

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                GetConstValueField(holderType, member.No).SetValue(holder, member.ConstValue);
            }

            // Provider
            foreach (var provider in providers)
            {
                GetProviderField(holderType, provider.Member.No).SetValue(holder, provider.Provider);
            }

            // Converter
            foreach (var converter in converters)
            {
                GetConverterField(holderType, converter.Member.No).SetValue(holder, converter.Converter);
            }

            // NullIf
            foreach (var member in context.Members.Where(x => x.IsNullIf))
            {
                GetNullIfValueField(holderType, member.No).SetValue(holder, member.NullIfValue);
            }

            return new HolderInfo(holder, hasDestinationParameter, hasContext);
        }

        private static bool IsDestinationParameterRequired(MapperCreateContext context)
        {
            if (context.BeforeMaps.Any(x => x.Type.HasDestinationParameter()) ||
                context.AfterMaps.Any(x => x.Type.HasDestinationParameter()))
            {
                return true;
            }

            return context.Members.Any(x => ((x.MapFrom is not null) && x.MapFrom.Type.HasDestinationParameter()) ||
                                            ((x.Condition is not null) && x.Condition.Type.HasDestinationParameter()));
        }

        private static bool IsContextRequired(MapperCreateContext context)
        {
            if ((context.Factory is not null) && context.Factory.Type.HasContext())
            {
                return true;
            }

            if (context.BeforeMaps.Any(x => x.Type.HasContext()) ||
                context.AfterMaps.Any(x => x.Type.HasContext()))
            {
                return true;
            }

            return context.Members.Any(x => ((x.MapFrom is not null) && x.MapFrom.Type.HasContext()) ||
                                            ((x.Condition is not null) && x.Condition.Type.HasContext()) ||
                                            ((x.Converter is not null) && x.Converter.Type.HasContext()));
        }

        private object? ResolveFactory(TypeEntry<FactoryType>? entry)
        {
            if (entry is null)
            {
                return null;
            }

            return entry.Type == FactoryType.InterfaceType ? serviceProvider.GetService((Type)entry.Value) : entry.Value;
        }

        private object ResolveAction(TypeEntry<ActionType> entry) =>
            entry.Type == ActionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        private object ResolveCondition(TypeEntry<ConditionType> entry) =>
            entry.Type == ConditionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        private object? ResolveProvider(FromTypeEntry entry)
        {
            switch (entry.Type)
            {
                case FromType.LazyFunc:
                    return entry.Value.GetType().GetProperty("Value")!.GetValue(entry.Value);
                case FromType.Func:
                case FromType.FuncContext:
                case FromType.Interface:
                    return entry.Value;
                case FromType.InterfaceType:
                    return serviceProvider.GetService((Type)entry.Value)!;
            }

            return null;
        }

        private object ResolveConverter(TypeEntry<ConverterType> entry) =>
             entry.Type == ConverterType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        //--------------------------------------------------------------------------------
        // Field
        //--------------------------------------------------------------------------------

        private static FieldInfo GetMapperField(Type holderType) => holderType.GetField("mapper")!;

        private static FieldInfo GetFactoryField(Type holderType) => holderType.GetField("factory")!;

        private static FieldInfo GetBeforeMapField(Type holderType, int index) => holderType.GetField($"beforeMap{index}")!;

        private static FieldInfo GetAfterMapField(Type holderType, int index) => holderType.GetField($"afterMap{index}")!;

        private static FieldInfo GetConditionField(Type holderType, int index) => holderType.GetField($"condition{index}")!;

        private static FieldInfo GetConstValueField(Type holderType, int index) => holderType.GetField($"constValue{index}")!;

        private static FieldInfo GetProviderField(Type holderType, int index) => holderType.GetField($"provider{index}")!;

        private static FieldInfo GetConverterField(Type holderType, int index) => holderType.GetField($"converter{index}")!;

        private static FieldInfo GetNullIfValueField(Type holderType, int index) => holderType.GetField($"nullIfValue{index}")!;

        //--------------------------------------------------------------------------------
        // Method
        //--------------------------------------------------------------------------------

        private static Delegate CreateMapAction(MapperCreateContext context, HolderInfo holderInfo)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapAction",
                typeof(void),
                new[] { holderInfo.Holder.GetType(), context.SourceType, context.DestinationType },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var workTable = new WorkTable(false, false);
            EmitPrepare(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.BeforeMaps, holderInfo, workTable, GetBeforeMapField);
            EmitMemberMapping(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.AfterMaps, holderInfo, workTable, GetAfterMapField);
            EmitReturn(ilGenerator, workTable);

            return dynamicMethod.CreateDelegate(
                typeof(Action<,>).MakeGenericType(
                    context.SourceType,
                    context.DestinationType),
                holderInfo.Holder);
        }

        private static Delegate CreateParameterMapAction(MapperCreateContext context, HolderInfo holderInfo)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "ParameterMapAction",
                typeof(void),
                new[] { holderInfo.Holder.GetType(), context.SourceType, context.DestinationType, typeof(object) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var workTable = new WorkTable(false, true);
            EmitPrepare(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.BeforeMaps, holderInfo, workTable, GetBeforeMapField);
            EmitMemberMapping(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.AfterMaps, holderInfo, workTable, GetAfterMapField);
            EmitReturn(ilGenerator, workTable);

            return dynamicMethod.CreateDelegate(
                typeof(Action<,,>).MakeGenericType(
                    context.SourceType,
                    context.DestinationType,
                    typeof(object)),
                holderInfo.Holder);
        }

        private static Delegate CreateMapFunc(MapperCreateContext context, HolderInfo holderInfo)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapFunc",
                context.DestinationType,
                new[] { holderInfo.Holder.GetType(), context.SourceType },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var workTable = new WorkTable(true, false);
            EmitPrepare(ilGenerator, context, holderInfo, workTable);
            EmitConstructor(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.BeforeMaps, holderInfo, workTable, GetBeforeMapField);
            EmitMemberMapping(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.AfterMaps, holderInfo, workTable, GetAfterMapField);
            EmitReturn(ilGenerator, workTable);

            return dynamicMethod.CreateDelegate(
                typeof(Func<,>).MakeGenericType(
                    context.SourceType,
                    context.DestinationType),
                holderInfo.Holder);
        }

        private static Delegate CreateParameterMapFunc(MapperCreateContext context, HolderInfo holderInfo)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapFunc",
                context.DestinationType,
                new[] { holderInfo.Holder.GetType(), context.SourceType, typeof(object) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var workTable = new WorkTable(true, true);
            EmitPrepare(ilGenerator, context, holderInfo, workTable);
            EmitConstructor(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.BeforeMaps, holderInfo, workTable, GetBeforeMapField);
            EmitMemberMapping(ilGenerator, context, holderInfo, workTable);
            EmitMapActions(ilGenerator, context.AfterMaps, holderInfo, workTable, GetAfterMapField);
            EmitReturn(ilGenerator, workTable);

            return dynamicMethod.CreateDelegate(
                typeof(Func<,,>).MakeGenericType(
                    context.SourceType,
                    typeof(object),
                    context.DestinationType),
                holderInfo.Holder);
        }

        //--------------------------------------------------------------------------------
        // Block
        //--------------------------------------------------------------------------------

        private static void EmitPrepare(ILGenerator ilGenerator, MapperCreateContext context, HolderInfo holderInfo, WorkTable work)
        {
            // Destination
            if (work.IsFunction && holderInfo.HasDestinationParameter)
            {
                // TODO Struct
                work.DestinationLocal = ilGenerator.DeclareLocal(context.DestinationType);
            }

            // Context
            if (holderInfo.HasContext)
            {
                work.ContextLocal = ilGenerator.DeclareLocal(typeof(ResolutionContext));

                ilGenerator.Emit(work.HasParameter ? (work.IsFunction ? OpCodes.Ldarg_2 : OpCodes.Ldarg_3) : OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, GetMapperField(holderInfo.Holder.GetType()));
                ilGenerator.Emit(OpCodes.Newobj, typeof(ResolutionContext).GetConstructor(new[] { typeof(object), typeof(INestedMapper) })!);
                ilGenerator.EmitStloc(work.ContextLocal);
            }

            // Temporary
            foreach (var member in context.Members)
            {
                if (member.IsNullIf && !member.Property.PropertyType.IsClass && !work.TemporaryLocals.ContainsKey(member.Property.PropertyType))
                {
                    work.TemporaryLocals[member.Property.PropertyType] = ilGenerator.DeclareLocal(member.Property.PropertyType);
                }
            }
        }

        private static void EmitConstructor(ILGenerator ilGenerator, MapperCreateContext context, HolderInfo holderInfo, WorkTable work)
        {
            if (context.IsFactoryUseServiceProvider)
            {
                // IServiceProvider
                var field = holderInfo.Holder.GetType().GetField("factory")!;
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, field!);
                ilGenerator.Emit(OpCodes.Ldtoken, context.DestinationType);
                ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
                var method = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService), new[] { typeof(Type) })!;
                ilGenerator.EmitCallMethod(method);
                ilGenerator.Emit(OpCodes.Castclass, context.DestinationType);
            }
            else if (context.Factory is not null)
            {
                var field = GetFactoryField(holderInfo.Holder.GetType());
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, field);

                switch (context.Factory.Type)
                {
                    case FactoryType.FuncDestination:
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceContextDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.Interface:
                    case FactoryType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Create")!);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported factory. type=[{field.FieldType}]");
                }
            }
            else
            {
                // Default constructor
                var ctor = context.DestinationType.GetConstructor(Type.EmptyTypes);
                if (ctor is null)
                {
                    throw new InvalidOperationException($"Type has not default constructor. type=[{context.DestinationType}]");
                }

                ilGenerator.Emit(OpCodes.Newobj, ctor);
            }

            if (work.DestinationLocal is not null)
            {
                ilGenerator.EmitStloc(work.DestinationLocal);
            }
        }

        private static void EmitMapActions(ILGenerator ilGenerator, IReadOnlyList<TypeEntry<ActionType>> actions, HolderInfo holderInfo, WorkTable work, Func<Type, int, FieldInfo> resolver)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                var field = resolver(holderInfo.Holder.GetType(), i)!;
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, field);

                switch (actions[i].Type)
                {
                    case ActionType.Action:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        EmitStackDestination(ilGenerator, work);    // Destination
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.ActionContext:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        EmitStackDestination(ilGenerator, work);    // Destination
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.Interface:
                    case ActionType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        EmitStackDestination(ilGenerator, work);    // Destination
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Process")!);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported action map. type=[{field.FieldType}]");
                }
            }
        }

        private static void EmitMemberMapping(ILGenerator ilGenerator, MapperCreateContext context, HolderInfo holderInfo, WorkTable work)
        {
            foreach (var member in context.Members)
            {
                var nextLabel = (member.Condition is not null) ? ilGenerator.DefineLabel() : default;

                // Condition
                if (member.Condition is not null)
                {
                    EmitConditionBranch(ilGenerator, holderInfo, work, member, nextLabel);
                }

                // Value expression
                if (member.IsConst)
                {
                    // Const
                    EmitStackDestination(ilGenerator, work);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, GetConstValueField(holderInfo.Holder.GetType(), member.No));
                    ilGenerator.EmitCallMethod(member.Property.SetMethod!);
                }
                else if (member.IsNested)
                {
                    // TODO Nest
                }
                else
                {
                    EmitStackDestination(ilGenerator, work);

                    EmitStackSourceMember(ilGenerator, holderInfo, work, member);

                    // TODO toNullable/fromNullable, conv-cast, conv-converter
                    if (member.Condition is not null)
                    {
                        // TODO
                    }
                    else if (member.IsNullIf)
                    {
                        var convert = !member.Property.PropertyType.IsAssignableFrom(member.MapFrom!.MemberType);
                        var setLabel = ilGenerator.DefineLabel();

                        if (member.Property.PropertyType.IsClass)
                        {
                            var reloadLabel = convert ? ilGenerator.DefineLabel() : default;

                            // Branch
                            ilGenerator.Emit(OpCodes.Dup);
                            ilGenerator.Emit(OpCodes.Brtrue_S, convert ? reloadLabel : setLabel);

                            // Null if
                            ilGenerator.Emit(OpCodes.Pop);
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldfld, GetNullIfValueField(holderInfo.Holder.GetType(), member.No));
                            if (convert)
                            {
                                // TODO S?
                                ilGenerator.Emit(OpCodes.Br_S, setLabel);

                                // Convert
                                ilGenerator.MarkLabel(reloadLabel);

                                throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            var reloadLabel = ilGenerator.DefineLabel();
                            var temporaryLocal = work.TemporaryLocals[member.Property.PropertyType];

                            ilGenerator.EmitStloc(temporaryLocal);

                            // Branch
                            ilGenerator.EmitLdloca(temporaryLocal);
                            ilGenerator.Emit(OpCodes.Call, member.Property.PropertyType.GetProperty("HasValue")!.GetMethod!);
                            ilGenerator.Emit(OpCodes.Brtrue_S, reloadLabel);

                            // Null if
                            ilGenerator.Emit(OpCodes.Ldarg_0);
                            ilGenerator.Emit(OpCodes.Ldfld, GetNullIfValueField(holderInfo.Holder.GetType(), member.No));
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
                    else
                    {
                        // TODO
                    }

                    ilGenerator.EmitCallMethod(member.Property.SetMethod!);
                }

                if (member.Condition is not null)
                {
                    // Condition next:
                    ilGenerator.MarkLabel(nextLabel);
                }
            }
        }

        private static void EmitConditionBranch(ILGenerator ilGenerator, HolderInfo holderInfo, WorkTable work, MemberMapping member, Label nextLabel)
        {
            var field = GetConditionField(holderInfo.Holder.GetType(), member.No);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);

            switch (member.Condition!.Type)
            {
                case ConditionType.FuncSource:
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.FuncSourceContext:
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.FuncSourceDestinationContext:
                    ilGenerator.Emit(OpCodes.Ldarg_1); // Source
                    EmitStackDestination(ilGenerator, work);    // Destination
                    ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Invoke")!);
                    break;
                case ConditionType.Interface:
                case ConditionType.InterfaceType:
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    EmitStackDestination(ilGenerator, work);    // Destination
                    ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                    ilGenerator.EmitCallMethod(field.FieldType.GetMethod("Eval")!);
                    break;
            }

            // TODO S check
            //ilGenerator.Emit(OpCodes.Brfalse_S, nextLabel);
            ilGenerator.Emit(OpCodes.Brfalse, nextLabel);
        }

        private static void EmitStackSourceMember(ILGenerator ilGenerator, HolderInfo holderInfo, WorkTable work, MemberMapping member)
        {
            switch (member.MapFrom!.Type)
            {
                case FromType.Properties:
                    ilGenerator.Emit(OpCodes.Ldarg_1); // Source
                    foreach (var pi in (PropertyInfo[])member.MapFrom.Value)
                    {
                        ilGenerator.EmitCallMethod(pi.GetMethod!);
                    }

                    break;
                case FromType.LazyFunc:
                    var lazyFuncField = GetProviderField(holderInfo.Holder.GetType(), member.No);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, lazyFuncField);
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    ilGenerator.EmitCallMethod(lazyFuncField.FieldType.GetMethod("Invoke")!);
                    break;
                case FromType.Func:
                    var funcField = GetProviderField(holderInfo.Holder.GetType(), member.No);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, funcField);
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    EmitStackDestination(ilGenerator, work);    // Destination
                    ilGenerator.EmitCallMethod(funcField.FieldType.GetMethod("Invoke")!);
                    break;
                case FromType.FuncContext:
                    var funcContextField = GetProviderField(holderInfo.Holder.GetType(), member.No);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, funcContextField);
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    EmitStackDestination(ilGenerator, work);    // Destination
                    ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                    ilGenerator.EmitCallMethod(funcContextField.FieldType.GetMethod("Invoke")!);
                    break;
                case FromType.Interface:
                case FromType.InterfaceType:
                    var interfaceField = GetProviderField(holderInfo.Holder.GetType(), member.No);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, interfaceField);
                    ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                    EmitStackDestination(ilGenerator, work);    // Destination
                    ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                    ilGenerator.EmitCallMethod(interfaceField.FieldType.GetMethod("Provide")!);
                    break;
            }
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        // TODO Source こっちも2つか？

        // TODO structで引数用とコール用？
        private static void EmitStackDestination(ILGenerator ilGenerator, WorkTable work)
        {
            if (work.IsFunction)
            {
                if (work.DestinationLocal is not null)
                {
                    ilGenerator.EmitLdloc(work.DestinationLocal);
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

        private static void EmitReturn(ILGenerator ilGenerator, WorkTable work)
        {
            if (work.DestinationLocal is not null)
            {
                ilGenerator.EmitLdloc(work.DestinationLocal);
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        private class HolderInfo
        {
            public object Holder { get; }

            public bool HasDestinationParameter { get; }

            public bool HasContext { get; }

            public HolderInfo(
                object holder,
                bool hasDestinationParameter,
                bool hasContext)
            {
                Holder = holder;
                HasDestinationParameter = hasDestinationParameter;
                HasContext = hasContext;
            }
        }

        private class WorkTable
        {
            public bool IsFunction { get; }

            public bool HasParameter { get; }

            public LocalBuilder? DestinationLocal { get; set; }

            public LocalBuilder? ContextLocal { get; set; }

            public Dictionary<Type, LocalBuilder> TemporaryLocals { get; } = new();

            public WorkTable(bool isFunction, bool hasParameter)
            {
                IsFunction = isFunction;
                HasParameter = hasParameter;
            }
        }
    }
}
