namespace Smart.Mapper.Mappers;

using System.Reflection;

using Smart.Mapper.Options;

internal sealed class MemberMapping
{
    public int No { get; }

    public MemberInfo Member { get; }

    public Type MemberType => Member is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)Member).FieldType;

    public TypeEntry<ConditionType>? Condition { get; }

    public FromTypeEntry? MapFrom { get; }

    public bool IsNested { get; }

    public string? NestedProfile { get; }

    public bool IsConst { get; }

    public object? ConstValue { get; }

    public ConverterEntry? Converter { get; }

    public bool IsNullIf { get; }

    public object? NullIfValue { get; }

    public MemberMapping(
        int no,
        MemberInfo member,
        TypeEntry<ConditionType>? condition,
        FromTypeEntry? mapFrom,
        bool isNested,
        string? nestedProfile,
        bool isConst,
        object? constValue,
        ConverterEntry? converter,
        bool isNullIf,
        object? nullIfValue)
    {
        No = no;
        Member = member;
        Condition = condition;
        MapFrom = mapFrom;
        IsNested = isNested;
        NestedProfile = nestedProfile;
        IsConst = isConst;
        ConstValue = constValue;
        Converter = converter;
        IsNullIf = isNullIf;
        NullIfValue = nullIfValue;
    }
}

internal sealed class MapperCreateContext
{
    private static readonly Func<string, string?> DefaultMatcher = static x => x;

    private readonly DefaultOption defaultOption;

    private readonly MappingOption mappingOption;

    public Type DelegateSourceType { get; }

    public Type DelegateDestinationType { get; }

    public Type MapDestinationType => mappingOption.DestinationType;

    // Factory

    public bool IsFactoryUseServiceProvider => mappingOption.IsFactoryUseServiceProvider() ||
                                               defaultOption.IsFactoryUseServiceProvider();

    public TypeEntry<FactoryType>? Factory { get; }

    // Map actions

    public IReadOnlyList<TypeEntry<ActionType>> BeforeMaps => mappingOption.GetBeforeMaps();

    public IReadOnlyList<TypeEntry<ActionType>> AfterMaps => mappingOption.GetAfterMaps();

    // Member

    public IReadOnlyList<MemberMapping> Members { get; }

    // Mapper

    public INestedMapper NestedMapper { get; }

    public MapperCreateContext(
        Type sourceType,
        Type destinationType,
        DefaultOption defaultOption,
        MappingOption mappingOption,
        INestedMapper nestedMapper)
    {
        DelegateSourceType = sourceType;
        DelegateDestinationType = destinationType;
        this.defaultOption = defaultOption;
        this.mappingOption = mappingOption;
        NestedMapper = nestedMapper;

        Factory = mappingOption.GetFactory();
        if (Factory is null)
        {
            var defaultFactory = defaultOption.GetFactory(mappingOption.DestinationType);
            if (defaultFactory is not null)
            {
                Factory = new TypeEntry<FactoryType>(FactoryType.FuncDestination, defaultFactory);
            }
        }

        var matcher = mappingOption.GetMatcher() ?? DefaultMatcher;

        var members = new List<MemberMapping>();
        foreach (var memberOption in this.mappingOption.MemberOptions.Where(static x => !x.IsIgnore()).OrderBy(static x => x.GetOrder()))
        {
            bool isConst;
            object? constValue;
            if (memberOption.UseConst())
            {
                isConst = true;
                constValue = memberOption.GetConstValue();
            }
            else
            {
                isConst = mappingOption.TryGetConstValue(memberOption.MemberType, out constValue);
                if (!isConst)
                {
                    isConst = defaultOption.TryGetConstValue(memberOption.MemberType, out constValue);
                }
            }

            if (isConst)
            {
                members.Add(new MemberMapping(
                    members.Count,
                    memberOption.Member,
                    memberOption.GetCondition(),
                    null,
                    false,
                    null,
                    true,
                    constValue,
                    (constValue is not null) ? ResolveConverter(constValue.GetType(), memberOption.MemberType) : null,
                    false,
                    null));
            }
            else
            {
                var mapFrom = ResolveMapFrom(memberOption, matcher);
                if (mapFrom is not null)
                {
                    bool isNullIf;
                    object? nullIfValue;
                    if (memberOption.UseNullIf())
                    {
                        isNullIf = true;
                        nullIfValue = memberOption.GetNullIfValue();
                    }
                    else
                    {
                        isNullIf = mappingOption.TryGetNullIfValue(memberOption.MemberType, out nullIfValue);
                        if (!isNullIf)
                        {
                            isNullIf = defaultOption.TryGetNullIfValue(memberOption.MemberType, out nullIfValue);
                        }
                    }

                    members.Add(new MemberMapping(
                        members.Count,
                        memberOption.Member,
                        memberOption.GetCondition(),
                        mapFrom,
                        memberOption.IsNested(),
                        memberOption.GetNestedProfile(),
                        false,
                        null,
                        ResolveConverter(mapFrom.MemberType, memberOption.MemberType),
                        isNullIf,
                        nullIfValue));
                }
            }
        }

        Members = members;
    }

    private FromTypeEntry? ResolveMapFrom(MemberOption memberOption, Func<string, string?> matcher)
    {
        var mapFrom = memberOption.GetMapFrom();
        if (mapFrom is not null)
        {
            return mapFrom;
        }

        var name = matcher(memberOption.Member.Name) ?? memberOption.Member.Name;
        var pi = mappingOption.SourceType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if ((pi is not null) && pi.CanRead)
        {
            return new FromTypeEntry(FromType.Path, pi.PropertyType, new MemberInfo[] { pi });
        }

        var fi = mappingOption.SourceType.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (fi is not null)
        {
            return new FromTypeEntry(FromType.Path, fi.FieldType, new MemberInfo[] { fi });
        }

        return null;
    }

    private ConverterEntry? ResolveConverter(Type sourceType, Type destinationType)
    {
        if (destinationType.IsAssignableFrom(sourceType))
        {
            return null;
        }

        // TS to TD : Func<TS, TD>
        var converter = mappingOption.GetConverter(sourceType, destinationType) ??
                        defaultOption.GetConverter(sourceType, destinationType);
        if (converter is not null)
        {
            return converter;
        }

        var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
        if (sourceUnderlyingType is not null)
        {
            // TS? to TD : Func<TS, TD>
            converter = mappingOption.GetConverter(sourceUnderlyingType, destinationType) ??
                        defaultOption.GetConverter(sourceUnderlyingType, destinationType);
            if (converter is not null)
            {
                return converter;
            }
        }

        var destinationUnderlyingType = Nullable.GetUnderlyingType(destinationType);
        if (destinationUnderlyingType is not null)
        {
            // TS to TD? : Func<TS, TD>
            converter = mappingOption.GetConverter(sourceType, destinationUnderlyingType) ??
                        defaultOption.GetConverter(sourceType, destinationUnderlyingType);
            if (converter is not null)
            {
                return converter;
            }

            if (sourceUnderlyingType is not null)
            {
                // TS? to TD? : Func<TS, TD>
                converter = mappingOption.GetConverter(sourceUnderlyingType, destinationUnderlyingType) ??
                            defaultOption.GetConverter(sourceUnderlyingType, destinationUnderlyingType);
                if (converter is not null)
                {
                    return converter;
                }
            }
        }

        if (sourceType.IsValueType && !sourceType.IsNullableType())
        {
            // TS to TD : Func<TS?, TD>
            var nullableSourceType = typeof(Nullable<>).MakeGenericType(sourceType);
            converter = mappingOption.GetConverter(nullableSourceType, destinationType) ??
                        defaultOption.GetConverter(nullableSourceType, destinationType);
            if (converter is not null)
            {
                return converter;
            }

            // TS to TD? : Func<TS?, TD>
            if (destinationUnderlyingType is not null)
            {
                converter = mappingOption.GetConverter(nullableSourceType, destinationUnderlyingType) ??
                            defaultOption.GetConverter(nullableSourceType, destinationUnderlyingType);
                if (converter is not null)
                {
                    return converter;
                }
            }
        }

        return null;
    }
}
