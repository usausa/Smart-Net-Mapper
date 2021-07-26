namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Linq;
    using Smart.Mapper.Components;
    using Smart.Reflection.Emit;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
    internal class EmitMapperFactory : IMapperFactory
    {
        private readonly IConverterResolver converterResolver;

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

        public EmitMapperFactory(
            IConverterResolver converterResolver,
            IServiceProvider serviceProvider)
        {
            this.converterResolver = converterResolver;
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

            // TODO Define field

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

            // TODO Set field

            return new HolderInfo(
                holder,
                hasDestinationParameter,
                hasContext,
                hasNestedMapper);
        }

        private static bool IsDestinationParameterRequired(MapperCreateContext context)
        {
            if (context.BeforeMaps.Any(x => x.Type.HasDestinationParameter()) ||
                context.AfterMaps.Any(x => x.Type.HasDestinationParameter()))
            {
                return true;
            }

            return context.Members.Any(x => x.MapFrom.Type.HasDestinationParameter() ||
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

            return context.Members.Any(x => x.MapFrom.Type.HasContext() ||
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

            if (entry.Type == FactoryType.InterfaceType)
            {
                return serviceProvider.GetService((Type)entry.Value);
            }

            return entry.Value;
        }

        private object ResolveAction(TypeEntry<ActionType> entry)
        {
            if (entry.Type == ActionType.InterfaceType)
            {
                return serviceProvider.GetService((Type)entry.Value)!;
            }

            return entry.Value;
        }

        //--------------------------------------------------------------------------------
        // Field
        //--------------------------------------------------------------------------------

        private static FieldInfo GetMapperField(Type holderType) => holderType.GetField("mapper")!;

        private static FieldInfo GetFactoryField(Type holderType) => holderType.GetField("factory")!;

        private static FieldInfo GetBeforeMapField(Type holderType, int index) => holderType.GetField($"beforeMap{index}")!;

        private static FieldInfo GetAfterMapField(Type holderType, int index) => holderType.GetField($"afterMap{index}")!;

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
            EmitMemberMapping(ilGenerator, context, workTable);
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
            EmitMemberMapping(ilGenerator, context, workTable);
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
            EmitMemberMapping(ilGenerator, context, workTable);
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
            EmitMemberMapping(ilGenerator, context, workTable);
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
                switch (context.Factory.Type)
                {
                    case FactoryType.FuncDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.FuncSourceContextDestination:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case FactoryType.Interface:
                    case FactoryType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
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
                switch (actions[i].Type)
                {
                    case ActionType.Action:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        EmitStackDestination(ilGenerator, work);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.ActionContext:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        EmitStackDestination(ilGenerator, work);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Invoke")!);
                        break;
                    case ActionType.Interface:
                    case ActionType.InterfaceType:
                        ilGenerator.Emit(OpCodes.Ldarg_0);
                        ilGenerator.Emit(OpCodes.Ldfld, field);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        EmitStackDestination(ilGenerator, work);
                        ilGenerator.EmitLdloc(work.ContextLocal!);
                        ilGenerator.Emit(OpCodes.Callvirt, field.FieldType.GetMethod("Process")!);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported action map. type=[{field.FieldType}]");
                }
            }
        }

        private static void EmitMemberMapping(ILGenerator ilGenerator, MapperCreateContext context, WorkTable work)
        {
            foreach (var member in context.Members)
            {
                // by Property
                if (member.MapFrom.Type == FromType.Property)
                {
                    var sourceProperty = (PropertyInfo)member.MapFrom.Value;
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
                        // Need convert
                        throw new NotImplementedException();
                    }
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

            public bool HasNestedMapper { get; }

            public HolderInfo(
                object holder,
                bool hasDestinationParameter,
                bool hasContext,
                bool hasNestedMapper)
            {
                Holder = holder;
                HasDestinationParameter = hasDestinationParameter;
                HasContext = hasContext;
                HasNestedMapper = hasNestedMapper;
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
