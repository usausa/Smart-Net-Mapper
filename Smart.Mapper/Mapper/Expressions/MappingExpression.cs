namespace Smart.Mapper.Expressions
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    using Smart.Mapper.Functions;
    using Smart.Mapper.Helpers;
    using Smart.Mapper.Options;

    internal sealed class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private readonly MappingOption mappingOption;

        public MappingExpression(MappingOption mappingOption)
        {
            this.mappingOption = mappingOption;
        }

        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        public IMappingExpression<TSource, TDestination> FactoryUsingServiceProvider()
        {
            mappingOption.SetFactoryUseServiceProvider();
            return this;
        }

        public IMappingExpression<TSource, TDestination> FactoryUsing(Func<TDestination> factory)
        {
            mappingOption.SetFactory(factory);
            return this;
        }

        public IMappingExpression<TSource, TDestination> FactoryUsing(Func<TSource, TDestination> factory)
        {
            mappingOption.SetFactory(factory);
            return this;
        }

        public IMappingExpression<TSource, TDestination> FactoryUsing(Func<TSource, ResolutionContext, TDestination> factory)
        {
            mappingOption.SetFactory(factory);
            return this;
        }

        public IMappingExpression<TSource, TDestination> FactoryUsing(IObjectFactory<TSource, TDestination> factory)
        {
            mappingOption.SetFactory(factory);
            return this;
        }

        public IMappingExpression<TSource, TDestination> FactoryUsing<TObjectFactory>()
            where TObjectFactory : IObjectFactory<TSource, TDestination>
        {
            mappingOption.SetFactory<TSource, TDestination, TObjectFactory>();
            return this;
        }

        //--------------------------------------------------------------------------------
        // Pre/Post process
        //--------------------------------------------------------------------------------

        public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action)
        {
            mappingOption.AddBeforeMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> BeforeMap(IMappingAction<TSource, TDestination> action)
        {
            mappingOption.AddBeforeMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> action)
        {
            mappingOption.AddBeforeMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>
        {
            mappingOption.AddBeforeMap<TSource, TDestination, TMappingAction>();
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
        {
            mappingOption.AddAfterMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap(IMappingAction<TSource, TDestination> action)
        {
            mappingOption.AddAfterMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> action)
        {
            mappingOption.AddAfterMap(action);
            return this;
        }

        public IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>
        {
            mappingOption.AddAfterMap<TSource, TDestination, TMappingAction>();
            return this;
        }

        //--------------------------------------------------------------------------------
        // Match
        //--------------------------------------------------------------------------------

        public IMappingExpression<TSource, TDestination> MatchMember(Func<string, string?> matcher)
        {
            mappingOption.SetMatcher(matcher);
            return this;
        }

        //--------------------------------------------------------------------------------
        // Member
        //--------------------------------------------------------------------------------

        public IMappingExpression<TSource, TDestination> ForAllMember(Action<IAllMemberExpression> option)
        {
            foreach (var memberOption in mappingOption.MemberOptions)
            {
                option(new AllMemberExpression(memberOption));
            }

            return this;
        }

        public IMappingExpression<TSource, TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> expression, Action<IMemberExpression<TSource, TDestination, TMember>> option)
        {
            var pi = ExpressionHelper.GetPrpPropertyInfo(expression);
            if (pi is null)
            {
                throw new ArgumentException("Invalid destination member expression.");
            }

            var memberOption = mappingOption.MemberOptions.FirstOrDefault(x => x.Property == pi);
            if (memberOption is null)
            {
                throw new ArgumentException("Invalid destination member expression.");
            }

            option(new MemberExpression<TSource, TDestination, TMember>(memberOption.Property, memberOption));
            return this;
        }

        //--------------------------------------------------------------------------------
        // Default
        //--------------------------------------------------------------------------------

        public IMappingExpression<TSource, TDestination> Default(Action<IMappingDefaultExpression> action)
        {
            action(new MappingDefaultExpression(mappingOption));
            return this;
        }
    }
}
