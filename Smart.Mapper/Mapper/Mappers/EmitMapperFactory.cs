namespace Smart.Mapper.Mappers;

using System.Reflection;
using System.Reflection.Emit;

#pragma warning disable CA1812
internal sealed class EmitMapperFactory : IMapperFactory
{
#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private readonly IServiceProvider serviceProvider;

    private int typeNo;

    private AssemblyBuilder? assemblyBuilder;

    private ModuleBuilder? moduleBuilder;

    private TypeBuilder DefineType()
    {
        lock (sync)
        {
            if (moduleBuilder is null)
            {
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("SmartMapperAssembly"),
                    AssemblyBuilderAccess.Run);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(
                    "SmartMapperModule");
            }

            var typeBuilder = moduleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            return typeBuilder;
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
        var typeBuilder = DefineType();

        var holder = new EmitHolderInfo(context, typeBuilder, serviceProvider);

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
        var type = typeof(MapperInfo<,>).MakeGenericType(context.DelegateSourceType, context.DelegateDestinationType);
        return Activator.CreateInstance(type)!;
    }

    //--------------------------------------------------------------------------------
    // Method
    //--------------------------------------------------------------------------------

    private static Delegate CreateMapAction(MapperCreateContext context, EmitHolderInfo holder)
    {
        // Func
        var dynamicMethod = new DynamicMethod(
            "MapAction",
            typeof(void),
            [holder.Instance.GetType(), context.DelegateSourceType, context.DelegateDestinationType],
            true);
        var ilGenerator = dynamicMethod.GetILGenerator();

        var builder = new EmitMapperBuilder(ilGenerator, context, holder, false, false);
        builder.Build();

        return dynamicMethod.CreateDelegate(
            typeof(Action<,>).MakeGenericType(
                context.DelegateSourceType,
                context.DelegateDestinationType),
            holder.Instance);
    }

    private static Delegate CreateParameterMapAction(MapperCreateContext context, EmitHolderInfo holder)
    {
        // Func
        var dynamicMethod = new DynamicMethod(
            "ParameterMapAction",
            typeof(void),
            [holder.Instance.GetType(), context.DelegateSourceType, context.DelegateDestinationType, typeof(object)],
            true);
        var ilGenerator = dynamicMethod.GetILGenerator();

        var builder = new EmitMapperBuilder(ilGenerator, context, holder, false, true);
        builder.Build();

        return dynamicMethod.CreateDelegate(
            typeof(Action<,,>).MakeGenericType(
                context.DelegateSourceType,
                context.DelegateDestinationType,
                typeof(object)),
            holder.Instance);
    }

    private static Delegate CreateMapFunc(MapperCreateContext context, EmitHolderInfo holder)
    {
        // Func
        var dynamicMethod = new DynamicMethod(
            "MapFunc",
            context.DelegateDestinationType,
            [holder.Instance.GetType(), context.DelegateSourceType],
            true);
        var ilGenerator = dynamicMethod.GetILGenerator();

        var builder = new EmitMapperBuilder(ilGenerator, context, holder, true, false);
        builder.Build();

        return dynamicMethod.CreateDelegate(
            typeof(Func<,>).MakeGenericType(
                context.DelegateSourceType,
                context.DelegateDestinationType),
            holder.Instance);
    }

    private static Delegate CreateParameterMapFunc(MapperCreateContext context, EmitHolderInfo holder)
    {
        // Func
        var dynamicMethod = new DynamicMethod(
            "MapFunc",
            context.DelegateDestinationType,
            [holder.Instance.GetType(), context.DelegateSourceType, typeof(object)],
            true);
        var ilGenerator = dynamicMethod.GetILGenerator();

        var builder = new EmitMapperBuilder(ilGenerator, context, holder, true, true);
        builder.Build();

        return dynamicMethod.CreateDelegate(
            typeof(Func<,,>).MakeGenericType(
                context.DelegateSourceType,
                typeof(object),
                context.DelegateDestinationType),
            holder.Instance);
    }
}
#pragma warning restore CA1812
