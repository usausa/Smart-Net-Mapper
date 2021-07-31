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

        // IConverterResolver, IFactoryResolver, IFunctionActivatorはDI

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
            var hasNestedMapper = hasContext || IsNestedExist(context);

            // Nested mapper
            if (hasNestedMapper)
            {
                typeBuilder.DefineField("mapper", typeof(INestedMapper), FieldAttributes.Public);
            }

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

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                typeBuilder.DefineField($"constValue{member.No}", member.Property.PropertyType, FieldAttributes.Public);
            }

            // Condition
            var conditions = context.Members.Where(x => x.Condition is not null)
                .Select(x => new { Member = x, Condition = ResolveCondition(x.Condition!) })
                .ToList();
            foreach (var condition in conditions)
            {
                typeBuilder.DefineField($"condition{condition.Member.No}", condition.Condition.GetType(), FieldAttributes.Public);
            }

            // Converter
            var converters = context.Members.Where(x => x.Converter is not null)
                .Select(x => new { Member = x, Converter = ResolveConverter(x.Converter!) })
                .ToList();
            foreach (var converter in converters)
            {
                typeBuilder.DefineField($"converter{converter.Member.No}", converter.Converter.GetType(), FieldAttributes.Public);
            }

            // TODO Define field :  null?

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // Nested mapper
            if (hasNestedMapper)
            {
                GetMapperField(holderType).SetValue(holder, context.NexMapper);
            }

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

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                GetConstValueField(holderType, member.No).SetValue(holder, member.ConstValue);
            }

            // Condition
            foreach (var condition in conditions)
            {
                GetConditionField(holderType, condition.Member.No).SetValue(holder, condition.Condition);
            }

            // Converter
            foreach (var converter in converters)
            {
                GetConverterField(holderType, converter.Member.No).SetValue(holder, converter.Converter);
            }

            // TODO Set field

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

        private static bool IsNestedExist(MapperCreateContext context) =>
            context.Members.Any(x => x.IsNested);

        private object? ResolveFactory(TypeEntry<FactoryType>? entry)
        {
            if (entry is null)
            {
                return null;
            }

            return entry.Type == FactoryType.InterfaceType ? serviceProvider.GetService((Type)entry.Value) : entry.Value;
        }

        private object ResolveAction(TypeEntry<ActionType> entry)
        {
            return entry.Type == ActionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;
        }

        private object ResolveCondition(TypeEntry<ConditionType> entry)
        {
            return entry.Type == ConditionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;
        }

        private object ResolveConverter(TypeEntry<ConverterType> entry)
        {
            return entry.Type == ConverterType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;
        }

        //--------------------------------------------------------------------------------
        // Field
        //--------------------------------------------------------------------------------

        private static FieldInfo GetMapperField(Type holderType) => holderType.GetField("mapper")!;

        private static FieldInfo GetFactoryField(Type holderType) => holderType.GetField("factory")!;

        private static FieldInfo GetBeforeMapField(Type holderType, int index) => holderType.GetField($"beforeMap{index}")!;

        private static FieldInfo GetAfterMapField(Type holderType, int index) => holderType.GetField($"afterMap{index}")!;

        private static FieldInfo GetConstValueField(Type holderType, int index) => holderType.GetField($"constValue{index}")!;

        private static FieldInfo GetConditionField(Type holderType, int index) => holderType.GetField($"condition{index}")!;

        private static FieldInfo GetConverterField(Type holderType, int index) => holderType.GetField($"converter{index}")!;

        // TODO

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
        // Helper
        //--------------------------------------------------------------------------------

        private static void EmitPrepare(ILGenerator ilGenerator, MapperCreateContext context, HolderInfo holderInfo, WorkTable work)
        {
            // Destination
            if (work.IsFunction && holderInfo.HasDestinationParameter)
            {
                work.DestinationLocal = ilGenerator.DeclareLocal(context.DestinationType);
            }

            // Context
            if (holderInfo.HasContext)
            {
                work.ContextLocal = ilGenerator.DeclareLocal(typeof(ResolutionContext));

                ilGenerator.Emit(work.HasParameter ? OpCodes.Ldarg_2 : OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, GetMapperField(holderInfo.Holder.GetType()));
                ilGenerator.Emit(OpCodes.Newobj, typeof(ResolutionContext).GetConstructor(new[] { typeof(object), typeof(INestedMapper) })!);
                ilGenerator.EmitStloc(work.ContextLocal);
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
                ilGenerator.Emit(OpCodes.Callvirt, method);
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
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceContextDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.Interface:
                    case FactoryType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Create")!);
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
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.ActionContext:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        EmitStackDestination(ilGenerator, work);    // Destination
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.Interface:
                    case ActionType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                        EmitStackDestination(ilGenerator, work);    // Destination
                        ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Process")!);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported action map. type=[{field.FieldType}]");
                }
            }
        }

        private static void EmitMemberMapping(ILGenerator ilGenerator, MapperCreateContext context, HolderInfo holderInfo, WorkTable work)
        {
            // TODO source ***
            // TODO nullIf
            // TODO nullIgnore
            // TODO toNullable/fromNullable, conv-cast, conv-converter
            foreach (var member in context.Members)
            {
                var nextLabel = (member.Condition is not null) ? ilGenerator.DefineLabel() : default;
                if (member.Condition is not null)
                {
                    var field = GetConditionField(holderInfo.Holder.GetType(), member.No);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    switch (member.Condition.Type)
                    {
                        case ConditionType.FuncSource:
                            ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                            ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                            break;
                        case ConditionType.FuncSourceContext:
                            ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                            ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                            ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                            break;
                        case ConditionType.FuncSourceDestinationContext:
                            ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                            EmitStackDestination(ilGenerator, work);    // Destination
                            ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                            ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                            break;
                        case ConditionType.Interface:
                        case ConditionType.InterfaceType:
                            ilGenerator.Emit(OpCodes.Ldarg_1);          // Source
                            EmitStackDestination(ilGenerator, work);    // Destination
                            ilGenerator.EmitLdloc(work.ContextLocal!);  // Context
                            ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Eval")!);
                            break;
                    }

                    ilGenerator.Emit(OpCodes.Brfalse_S, nextLabel);
                }

                if (member.IsConst)
                {
                    // Const
                    EmitStackDestination(ilGenerator, work);
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, GetConstValueField(holderInfo.Holder.GetType(), member.No));
                    ilGenerator.Emit(OpCodes.Callvirt, member.Property.SetMethod!);
                }
                else
                {
                    // TODO
                    // by Properties
                    if (member.MapFrom!.Type == FromType.Properties)
                    {
                        var sourceProperty = ((PropertyInfo[])member.MapFrom.Value)[0];
                        // Can set
                        if (member.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                        {
                            EmitStackDestination(ilGenerator, work);
                            ilGenerator.Emit(OpCodes.Ldarg_1);
                            ilGenerator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                            ilGenerator.Emit(OpCodes.Callvirt, member.Property.SetMethod!);
                        }
                        else
                        {
                            // TODO Need convert
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        // TODO others
                        throw new NotImplementedException();
                    }
                }

                if (member.Condition is not null)
                {
                    // Condition next:
                    ilGenerator.MarkLabel(nextLabel);
                }
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

        //--------------------------------------------------------------------------------
        // Data
        //--------------------------------------------------------------------------------

        private class HolderInfo
        {
            public object Holder { get; }

            public bool HasDestinationParameter { get; }

            public bool HasContext { get; }

            // TODO 優先再チェック NestはあるけどContextがないパターン あえて判別はいらないか？
            //public bool HasNestedMapper { get; }

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

            public WorkTable(bool isFunction, bool hasParameter)
            {
                IsFunction = isFunction;
                HasParameter = hasParameter;
            }
        }
    }
}
