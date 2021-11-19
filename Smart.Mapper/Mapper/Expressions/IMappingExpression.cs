namespace Smart.Mapper.Expressions;

using System;
using System.Linq.Expressions;

using Smart.Mapper.Functions;

public interface IMappingExpression<TSource, TDestination>
{
    //--------------------------------------------------------------------------------
    // Factory
    //--------------------------------------------------------------------------------

    IMappingExpression<TSource, TDestination> FactoryUsingServiceProvider();

    IMappingExpression<TSource, TDestination> FactoryUsing(Func<TDestination> factory);

    IMappingExpression<TSource, TDestination> FactoryUsing(Func<TSource, TDestination> factory);

    IMappingExpression<TSource, TDestination> FactoryUsing(Func<TSource, ResolutionContext, TDestination> factory);

    IMappingExpression<TSource, TDestination> FactoryUsing(IObjectFactory<TSource, TDestination> factory);

    IMappingExpression<TSource, TDestination> FactoryUsing<TObjectFactory>()
        where TObjectFactory : IObjectFactory<TSource, TDestination>;

    //--------------------------------------------------------------------------------
    // Pre/Post process
    //--------------------------------------------------------------------------------

    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> action);

    IMappingExpression<TSource, TDestination> BeforeMap(IMappingAction<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>()
        where TMappingAction : IMappingAction<TSource, TDestination>;

    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> action);

    IMappingExpression<TSource, TDestination> AfterMap(IMappingAction<TSource, TDestination> action);

    IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
        where TMappingAction : IMappingAction<TSource, TDestination>;

    //--------------------------------------------------------------------------------
    // Match
    //--------------------------------------------------------------------------------

    IMappingExpression<TSource, TDestination> MatchMember(Func<string, string?> matcher);

    //--------------------------------------------------------------------------------
    // Member
    //--------------------------------------------------------------------------------

    IMappingExpression<TSource, TDestination> ForAllMember(Action<IAllMemberExpression> option);

    IMappingExpression<TSource, TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> expression, Action<IMemberExpression<TSource, TDestination, TMember>> option);

    //--------------------------------------------------------------------------------
    // Default
    //--------------------------------------------------------------------------------

    IMappingExpression<TSource, TDestination> Default(Action<IMappingDefaultExpression> option);
}
