namespace Smart.Mapper.Mappers;

using System.Reflection;
using System.Reflection.Emit;

internal sealed class EmitHolderInfo
{
    public object Instance { get; }

    public bool DestinationParameterRequired { get; }

    public bool ContextRequired { get; }

    public EmitHolderInfo(
        MapperCreateContext context,
        TypeBuilder typeBuilder,
        IServiceProvider serviceProvider)
    {
        DestinationParameterRequired = IsDestinationParameterRequired(context);
        ContextRequired = IsContextRequired(context);

        // Mapper
        if (ContextRequired)
        {
            typeBuilder.DefineField("mapper", typeof(INestedMapper), FieldAttributes.Public);
        }

        // Factory
        var factory = ResolveFactory(serviceProvider, context.Factory);
        if (context.IsFactoryUseServiceProvider)
        {
            typeBuilder.DefineField("factory", typeof(IServiceProvider), FieldAttributes.Public);
        }
        else if (factory is not null)
        {
            typeBuilder.DefineField("factory", factory.GetType(), FieldAttributes.Public);
        }

        // BeforeMap
        var beforeMaps = context.BeforeMaps.Select(x => ResolveAction(serviceProvider, x)).ToList();
        for (var i = 0; i < beforeMaps.Count; i++)
        {
            typeBuilder.DefineField($"beforeMap{i}", beforeMaps[i].GetType(), FieldAttributes.Public);
        }

        // AfterMap
        var afterMaps = context.AfterMaps.Select(x => ResolveAction(serviceProvider, x)).ToList();
        for (var i = 0; i < afterMaps.Count; i++)
        {
            typeBuilder.DefineField($"afterMap{i}", afterMaps[i].GetType(), FieldAttributes.Public);
        }

        // Condition
        var conditions = context.Members.Where(x => x.Condition is not null)
            .Select(x => new { Member = x, Condition = ResolveCondition(serviceProvider, x.Condition!) })
            .ToList();
        foreach (var condition in conditions)
        {
            typeBuilder.DefineField($"condition{condition.Member.No}", condition.Condition.GetType(), FieldAttributes.Public);
        }

        // Const
        foreach (var member in context.Members.Where(x => x.IsConst))
        {
            typeBuilder.DefineField($"constValue{member.No}", member.MemberType, FieldAttributes.Public);
        }

        // Provider
        var providers = context.Members.Where(x => x.MapFrom is not null)
            .Select(x => new { Member = x, Provider = ResolveProvider(serviceProvider, x.MapFrom!) })
            .Where(x => x.Provider is not null)
            .ToList();
        foreach (var provider in providers)
        {
            typeBuilder.DefineField($"provider{provider.Member.No}", provider.Provider!.GetType(), FieldAttributes.Public);
        }

        // Nested
        var nestedMappers = context.Members.Where(x => x.IsNested)
            .Select(x => new { Member = x, Mapper = ResolveNestedMapper(context, x, false) })
            .ToList();
        foreach (var nestedMapper in nestedMappers)
        {
            typeBuilder.DefineField($"nestedMapper{nestedMapper.Member.No}", nestedMapper.Mapper.GetType(), FieldAttributes.Public);
        }

        var parameterNestedMappers = context.Members.Where(x => x.IsNested)
            .Select(x => new { Member = x, Mapper = ResolveNestedMapper(context, x, true) })
            .ToList();
        foreach (var nestedMapper in parameterNestedMappers)
        {
            typeBuilder.DefineField($"parameterNestedMapper{nestedMapper.Member.No}", nestedMapper.Mapper.GetType(), FieldAttributes.Public);
        }

        // Converter
        var converters = context.Members.Where(x => x.Converter is not null)
            .Select(x => new { Member = x, Converter = ResolveConverter(serviceProvider, x.Converter!) })
            .ToList();
        foreach (var converter in converters)
        {
            typeBuilder.DefineField($"converter{converter.Member.No}", converter.Converter.GetType(), FieldAttributes.Public);
        }

        // NullIf
        foreach (var member in context.Members.Where(x => x.IsNullIf))
        {
            typeBuilder.DefineField($"nullIfValue{member.No}", member.MemberType, FieldAttributes.Public);
        }

        // Create holder
        var typeInfo = typeBuilder.CreateTypeInfo();
        // ReSharper disable once RedundantSuppressNullableWarningExpression <= net6.0
        var holderType = typeInfo!.AsType();
        Instance = Activator.CreateInstance(holderType)!;

        // Mapper
        if (ContextRequired)
        {
            GetMapperField().SetValue(Instance, context.NestedMapper);
        }

        // Factory
        if (context.IsFactoryUseServiceProvider)
        {
            GetFactoryField().SetValue(Instance, serviceProvider);
        }
        else if (factory is not null)
        {
            GetFactoryField().SetValue(Instance, factory);
        }

        // BeforeMap
        for (var i = 0; i < beforeMaps.Count; i++)
        {
            GetBeforeMapField(i).SetValue(Instance, beforeMaps[i]);
        }

        // AfterMap
        for (var i = 0; i < afterMaps.Count; i++)
        {
            GetAfterMapField(i).SetValue(Instance, afterMaps[i]);
        }

        // Condition
        foreach (var condition in conditions)
        {
            GetConditionField(condition.Member.No).SetValue(Instance, condition.Condition);
        }

        // Const
        foreach (var member in context.Members.Where(x => x.IsConst))
        {
            GetConstValueField(member.No).SetValue(Instance, member.ConstValue);
        }

        // Provider
        foreach (var provider in providers)
        {
            GetProviderField(provider.Member.No).SetValue(Instance, provider.Provider);
        }

        // Nested
        foreach (var nestedMapper in nestedMappers)
        {
            GetNestedMapperField(nestedMapper.Member.No).SetValue(Instance, nestedMapper.Mapper);
        }

        foreach (var nestedMapper in parameterNestedMappers)
        {
            GetParameterNestedMapperField(nestedMapper.Member.No).SetValue(Instance, nestedMapper.Mapper);
        }

        // Converter
        foreach (var converter in converters)
        {
            GetConverterField(converter.Member.No).SetValue(Instance, converter.Converter);
        }

        // NullIf
        foreach (var member in context.Members.Where(x => x.IsNullIf))
        {
            GetNullIfValueField(member.No).SetValue(Instance, member.NullIfValue);
        }
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

    //--------------------------------------------------------------------------------
    // Resolve
    //--------------------------------------------------------------------------------

    private static object? ResolveFactory(IServiceProvider serviceProvider, TypeEntry<FactoryType>? entry)
    {
        if (entry is null)
        {
            return null;
        }

        return entry.Type == FactoryType.InterfaceType ? serviceProvider.GetService((Type)entry.Value) : entry.Value;
    }

    private static object ResolveAction(IServiceProvider serviceProvider, TypeEntry<ActionType> entry) =>
        entry.Type == ActionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

    private static object ResolveCondition(IServiceProvider serviceProvider, TypeEntry<ConditionType> entry) =>
        entry.Type == ConditionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

    private static object? ResolveProvider(IServiceProvider serviceProvider, FromTypeEntry entry)
    {
        return entry.Type switch
        {
            FromType.LazyFunc => entry.Value.GetType().GetProperty("Value")!.GetValue(entry.Value),
            FromType.Func => entry.Value,
            FromType.FuncContext => entry.Value,
            FromType.Interface => entry.Value,
            FromType.InterfaceType => serviceProvider.GetService((Type)entry.Value)!,
            _ => null
        };
    }

    private static object ResolveNestedMapper(MapperCreateContext context, MemberMapping member, bool hasParameter)
    {
        var argumentTypes = String.IsNullOrEmpty(member.NestedProfile) ? Type.EmptyTypes : new[] { typeof(string) };
        var method = hasParameter
            ? typeof(INestedMapper).GetMethod(nameof(INestedMapper.GetParameterMapperFunc), argumentTypes)!
            : typeof(INestedMapper).GetMethod(nameof(INestedMapper.GetMapperFunc), argumentTypes)!;
        var genericMethod = method.MakeGenericMethod(member.MapFrom!.MemberType, member.MemberType);
        return genericMethod.Invoke(context.NestedMapper, argumentTypes.Length == 0 ? null : new object?[] { member.NestedProfile })!;
    }

    private static object ResolveConverter(IServiceProvider serviceProvider, ConverterEntry entry) =>
         entry.Type == ConverterType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

    //--------------------------------------------------------------------------------
    // Field
    //--------------------------------------------------------------------------------

    public FieldInfo GetMapperField() => Instance.GetType().GetField("mapper")!;

    public FieldInfo GetFactoryField() => Instance.GetType().GetField("factory")!;

    public FieldInfo GetBeforeMapField(int index) => Instance.GetType().GetField($"beforeMap{index}")!;

    public FieldInfo GetAfterMapField(int index) => Instance.GetType().GetField($"afterMap{index}")!;

    public FieldInfo GetConditionField(int index) => Instance.GetType().GetField($"condition{index}")!;

    public FieldInfo GetConstValueField(int index) => Instance.GetType().GetField($"constValue{index}")!;

    public FieldInfo GetProviderField(int index) => Instance.GetType().GetField($"provider{index}")!;

    public FieldInfo GetNestedMapperField(int index) => Instance.GetType().GetField($"nestedMapper{index}")!;

    public FieldInfo GetParameterNestedMapperField(int index) => Instance.GetType().GetField($"parameterNestedMapper{index}")!;

    public FieldInfo GetConverterField(int index) => Instance.GetType().GetField($"converter{index}")!;

    public FieldInfo GetNullIfValueField(int index) => Instance.GetType().GetField($"nullIfValue{index}")!;
}
