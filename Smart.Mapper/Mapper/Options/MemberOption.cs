namespace Smart.Mapper.Options;

using System.Linq.Expressions;
using System.Reflection;

using Smart.Mapper.Functions;
using Smart.Mapper.Mappers;

public sealed class MemberOption
{
    public MemberInfo Member { get; }

    public Type MemberType => Member is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)Member).FieldType;

    private bool ignore;

    private bool nested;
    private string? nestedProfile;

    private int order = Int32.MaxValue;

    private TypeEntry<ConditionType>? condition;

    private FromTypeEntry? mapFrom;

    private bool useConst;
    private object? constValue;

    private bool useNullIf;
    private object? nullIfValue;

    public MemberOption(MemberInfo member)
    {
        Member = member;
    }

    //--------------------------------------------------------------------------------
    // Ignore
    //--------------------------------------------------------------------------------

    public void SetIgnore() => ignore = true;

    //--------------------------------------------------------------------------------
    // Nested
    //--------------------------------------------------------------------------------

    public void SetNested(string? profile)
    {
        nested = true;
        nestedProfile = profile;
    }

    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    public void SetOrder(int value) => order = value;

    //--------------------------------------------------------------------------------
    // Condition
    //--------------------------------------------------------------------------------

    public void SetCondition<TSource>(Func<TSource, bool> value) =>
        condition = new TypeEntry<ConditionType>(ConditionType.FuncSource, value);

    public void SetCondition<TSource>(Func<TSource, ResolutionContext, bool> value) =>
        condition = new TypeEntry<ConditionType>(ConditionType.FuncSourceContext, value);

    public void SetCondition<TSource, TDestination>(Func<TSource, TDestination, ResolutionContext, bool> value) =>
        condition = new TypeEntry<ConditionType>(ConditionType.FuncSourceDestinationContext, value);

    public void SetCondition<TSource, TDestination>(IMemberCondition<TSource, TDestination> value) =>
        condition = new TypeEntry<ConditionType>(ConditionType.Interface, value);

    public void SetCondition<TMemberCondition>() =>
        condition = new TypeEntry<ConditionType>(ConditionType.InterfaceType, typeof(TMemberCondition));

    //--------------------------------------------------------------------------------
    // MapFrom
    //--------------------------------------------------------------------------------

    public void SetMapFrom<TSource, TSourceMember>(Expression<Func<TSource, TSourceMember>> value)
    {
        if (value.Body is MemberExpression memberExpression)
        {
            var type = typeof(TSource);
            if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type))
            {
                mapFrom = new FromTypeEntry(FromType.Path, pi.PropertyType, new MemberInfo[] { pi });
                return;
            }

            if ((memberExpression.Member is FieldInfo fi) && (fi.ReflectedType == type))
            {
                mapFrom = new FromTypeEntry(FromType.Path, fi.FieldType, new MemberInfo[] { fi });
                return;
            }
        }

        mapFrom = new FromTypeEntry(FromType.LazyFunc, typeof(TSourceMember), new Lazy<Func<TSource, TSourceMember>>(value.Compile));
    }

    public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, TSourceMember> func) =>
        mapFrom = new FromTypeEntry(FromType.Func, typeof(TSourceMember), func);

    public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func) =>
        mapFrom = new FromTypeEntry(FromType.FuncContext, typeof(TSourceMember), func);

    public void SetMapFrom<TSource, TDestination, TMember>(IValueProvider<TSource, TDestination, TMember> value) =>
        mapFrom = new FromTypeEntry(FromType.Interface, typeof(TMember), value);

    public void SetMapFrom<TSource, TDestination, TMember, TValueResolver>()
        where TValueResolver : IValueProvider<TSource, TDestination, TMember> =>
        mapFrom = new FromTypeEntry(FromType.InterfaceType, typeof(TMember), typeof(TValueResolver));

    public void SetMapFrom<TSource>(string sourcePath)
    {
        if (String.IsNullOrEmpty(sourcePath))
        {
            throw new ArgumentException("Invalid source.", nameof(sourcePath));
        }

        var type = typeof(TSource);
        var members = new List<MemberInfo>();
        foreach (var name in sourcePath.Split("."))
        {
            var pi = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (pi is not null)
            {
                members.Add(pi);
                type = pi.PropertyType;
                continue;
            }

            var fi = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (fi is not null)
            {
                members.Add(fi);
                type = fi.FieldType;
                continue;
            }

            throw new ArgumentException("Invalid source.", nameof(sourcePath));
        }

        mapFrom = new FromTypeEntry(FromType.Path, type, members.ToArray());
    }

    //--------------------------------------------------------------------------------
    // Const
    //--------------------------------------------------------------------------------

    public void SetConstValue<TMember>(TMember value)
    {
        useConst = true;
        constValue = value;
    }

    //--------------------------------------------------------------------------------
    // Null
    //--------------------------------------------------------------------------------

    public void SetNullIfValue<TMember>(TMember value)
    {
        useNullIf = true;
        nullIfValue = value;
    }

    //--------------------------------------------------------------------------------
    // Internal
    //--------------------------------------------------------------------------------

    internal bool IsIgnore() => ignore;

    internal bool IsNested() => nested;

    internal string? GetNestedProfile() => nestedProfile;

    internal int GetOrder() => order;

    internal TypeEntry<ConditionType>? GetCondition() => condition;

    internal FromTypeEntry? GetMapFrom() => mapFrom;

    internal bool UseConst() => useConst;

    internal object? GetConstValue() => constValue;

    internal bool UseNullIf() => useNullIf;

    internal object? GetNullIfValue() => nullIfValue;
}
