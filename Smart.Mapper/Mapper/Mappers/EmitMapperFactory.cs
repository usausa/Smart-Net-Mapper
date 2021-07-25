namespace Smart.Mapper.Mappers
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Linq;
    using Smart.Mapper.Components;

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
            var factory = ResolveFactory(context);
            if (context.IsFactoryUseServiceProvider)
            {
                typeBuilder.DefineField("factory", typeof(IServiceProvider), FieldAttributes.Public);
            }
            else if (factory is not null)
            {
                typeBuilder.DefineField("factory", factory.GetType(), FieldAttributes.Public);
            }

            // BeforeMap
            // TODO

            // AfterMap
            // TODO

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
            else if (context.Factory is not null)
            {
                GetFactoryField(holderType).SetValue(holder, factory);
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

        private object? ResolveFactory(MapperCreateContext context)
        {
            if (context.Factory is null)
            {
                return null;
            }

            if (context.Factory.Type == FactoryType.InterfaceType)
            {
                return serviceProvider.GetService((Type)context.Factory.Value);
            }

            return context.Factory.Value;
        }

        //--------------------------------------------------------------------------------
        // Field
        //--------------------------------------------------------------------------------

        private static FieldInfo GetMapperField(Type holderType) => holderType.GetField("mapper")!;

        private static FieldInfo GetFactoryField(Type holderType) => holderType.GetField("factory")!;

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

            var workTable = new WorkTable { IsFunction = false };

            // TODO ResolutionContext

            // Property
            EmitMemberMapping(context, ilGenerator, workTable);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

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

            var workTable = new WorkTable { IsFunction = false };

            // TODO ResolutionContext

            // Property
            EmitMemberMapping(context, ilGenerator, workTable);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

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

            var workTable = new WorkTable { IsFunction = true };

            // TODO ResolutionContext

            // Class new
            EmitConstructor(context, ilGenerator, holderInfo);

            // Property
            EmitMemberMapping(context, ilGenerator, workTable);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

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

            var workTable = new WorkTable { IsFunction = true };

            // TODO ResolutionContext

            // Class new
            EmitConstructor(context, ilGenerator, holderInfo);

            // Property
            EmitMemberMapping(context, ilGenerator, workTable);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

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

        private static void EmitConstructor(MapperCreateContext context, ILGenerator ilGenerator, HolderInfo holderInfo)
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
                // TODO
                //var field = holderInfo.Holder.GetType().GetField("factory")!;
                //if (field.FieldType.IsGenericType && field.FieldType.GetGenericArguments()[0].IsAssignableFrom())
                //{
                //    // Func<Destination>
                //    // TODO
                //}
                //else if (field.FieldType == typeof(Func<,>).MakeGenericType(context.DestinationType))
                //{
                //    // Func<Source, Destination>
                //    // TODO
                //}
                //else if (field.FieldType == typeof(Func<,,>).MakeGenericType(context.DestinationType))
                //{
                //    // Func<Source, ResolutionContext, Destination>
                //    // TODO
                //}
                //else if (field.FieldType == typeof(IObjectFactory<,>).MakeGenericType(context.DestinationType))
                //{
                //    // IObjectFactory<Source, Destination>
                //    // TODO
                //}
                //else
                //{
                //    throw new InvalidOperationException($"Type has not default constructor. type=[{context.DestinationType}]");
                //}
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

            // TODO local?
        }

        private static void EmitMemberMapping(MapperCreateContext context, ILGenerator ilGenerator, WorkTable work)
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
                        // TODO local ?
                        ilGenerator.Emit(work.IsFunction ? OpCodes.Dup : OpCodes.Ldarg_2);
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

            // TODO Local等
        }

        private class WorkTable
        {
            public bool IsFunction { get; set; }
        }
    }
}
