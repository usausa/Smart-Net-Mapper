namespace Smart.Mapper.Mappers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Mapper.Components;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Ignore")]
    internal class EmitMapperFactory : IMapperFactory
    {
        private readonly IFactoryResolver factoryResolver;

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
            IFactoryResolver factoryResolver,
            IConverterResolver converterResolver,
            IServiceProvider serviceProvider)
        {
            this.factoryResolver = factoryResolver;
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
            var type = typeof(MapperInfo<,>).MakeGenericType(context.MappingOption.SourceType, context.MappingOption.DestinationType);
            return Activator.CreateInstance(type)!;
        }

        //--------------------------------------------------------------------------------
        // Holder
        //--------------------------------------------------------------------------------

        private object CreateHolder(MapperCreateContext context)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            // TODO Define field

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // TODO Set field

            return holder;
        }

        //--------------------------------------------------------------------------------
        // Method
        //--------------------------------------------------------------------------------

        private static Delegate CreateMapAction(MapperCreateContext context, object holder)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapAction",
                typeof(void),
                new[] { holder.GetType(), context.MappingOption.SourceType, context.MappingOption.DestinationType },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // TODO
            // Property
            foreach (var memberOption in context.MappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                var sourceProperty = context.MappingOption.SourceType.GetProperty(memberOption.Property.Name, BindingFlags.Instance | BindingFlags.Public);
                if ((sourceProperty is not null) && sourceProperty.CanRead)
                {
                    if (memberOption.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_2);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                        ilGenerator.Emit(OpCodes.Callvirt, memberOption.Property.SetMethod!);
                    }
                }
            }

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(
                typeof(Action<,>).MakeGenericType(
                    context.MappingOption.SourceType,
                    context.MappingOption.DestinationType),
                holder);
        }

        private static Delegate CreateParameterMapAction(MapperCreateContext context, object holder)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "ParameterMapAction",
                typeof(void),
                new[] { holder.GetType(), context.MappingOption.SourceType, context.MappingOption.DestinationType, typeof(object) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // TODO
            foreach (var memberOption in context.MappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                var sourceProperty = context.MappingOption.SourceType.GetProperty(memberOption.Property.Name, BindingFlags.Instance | BindingFlags.Public);
                if ((sourceProperty is not null) && sourceProperty.CanRead)
                {
                    if (memberOption.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_2);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                        ilGenerator.Emit(OpCodes.Callvirt, memberOption.Property.SetMethod!);
                    }
                }
            }

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(
                typeof(Action<,,>).MakeGenericType(
                    context.MappingOption.SourceType,
                    context.MappingOption.DestinationType,
                    typeof(object)),
                holder);
        }

        private static Delegate CreateMapFunc(MapperCreateContext context, object holder)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapFunc",
                context.MappingOption.DestinationType,
                new[] { holder.GetType(), context.MappingOption.SourceType },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Class new
            var ctor = context.MappingOption.DestinationType.GetConstructor(Type.EmptyTypes)!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            // TODO
            // Property
            foreach (var memberOption in context.MappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                var sourceProperty = context.MappingOption.SourceType.GetProperty(memberOption.Property.Name, BindingFlags.Instance | BindingFlags.Public);
                if ((sourceProperty is not null) && sourceProperty.CanRead)
                {
                    if (memberOption.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                        ilGenerator.Emit(OpCodes.Callvirt, memberOption.Property.SetMethod!);
                    }
                }
            }

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(
                typeof(Func<,>).MakeGenericType(
                    context.MappingOption.SourceType,
                    context.MappingOption.DestinationType),
                holder);
        }

        private static Delegate CreateParameterMapFunc(MapperCreateContext context, object holder)
        {
            // Func
            var dynamicMethod = new DynamicMethod(
                "MapFunc",
                context.MappingOption.DestinationType,
                new[] { holder.GetType(), context.MappingOption.SourceType, typeof(object) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Class new
            var ctor = context.MappingOption.DestinationType.GetConstructor(Type.EmptyTypes)!;
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            // TODO
            // Property
            foreach (var memberOption in context.MappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                var sourceProperty = context.MappingOption.SourceType.GetProperty(memberOption.Property.Name, BindingFlags.Instance | BindingFlags.Public);
                if ((sourceProperty is not null) && sourceProperty.CanRead)
                {
                    if (memberOption.Property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldarg_1);
                        ilGenerator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                        ilGenerator.Emit(OpCodes.Callvirt, memberOption.Property.SetMethod!);
                    }
                }
            }

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate(
                typeof(Func<,,>).MakeGenericType(
                    context.MappingOption.SourceType,
                    typeof(object),
                    context.MappingOption.DestinationType),
                holder);
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        // TODO コンストラクターセレクター？、Factoryと一緒にする？、デフォルトCtor前提で良い(AutoMapperはそう)
        // TODO コンテキストの必要性チェック
        // TODO 中間構造の作成？
    }
}
