namespace Smart.Mapper.Expressions;

using System.Linq.Expressions;
using System.Reflection;

using Smart.Mapper.Functions;

public interface IMemberExpression<TSource, out TDestination, in TMember>
{
    //--------------------------------------------------------------------------------
    // Info
    //--------------------------------------------------------------------------------

    MemberInfo DestinationMember { get; }

    //--------------------------------------------------------------------------------
    // Ignore
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> Ignore();

    //--------------------------------------------------------------------------------
    // Nest
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> Nested();

    IMemberExpression<TSource, TDestination, TMember> Nested(string profile);

    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> Order(int order);

    //--------------------------------------------------------------------------------
    // Condition
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition);

    IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, ResolutionContext, bool> condition);

    IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, ResolutionContext, bool> condition);

    IMemberExpression<TSource, TDestination, TMember> Condition(IMemberCondition<TSource, TDestination> condition);

    IMemberExpression<TSource, TDestination, TMember> Condition<TMemberCondition>()
        where TMemberCondition : IMemberCondition<TSource, TDestination>;

    //--------------------------------------------------------------------------------
    // MapFrom
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> expression);

    IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> func);

    IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func);

    IMemberExpression<TSource, TDestination, TMember> MapFrom(IValueProvider<TSource, TDestination, TMember> resolver);

    IMemberExpression<TSource, TDestination, TMember> MapFrom<TValueResolver>()
        where TValueResolver : IValueProvider<TSource, TDestination, TMember>;

    IMemberExpression<TSource, TDestination, TMember> MapFrom(string sourcePath);

    //--------------------------------------------------------------------------------
    // Constant
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> Const(TMember value);

    //--------------------------------------------------------------------------------
    // Null
    //--------------------------------------------------------------------------------

    IMemberExpression<TSource, TDestination, TMember> NullIf(TMember value);
}
