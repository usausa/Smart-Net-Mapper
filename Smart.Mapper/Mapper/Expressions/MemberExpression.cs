namespace Smart.Mapper.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    using Smart.Mapper.Functions;
    using Smart.Mapper.Options;

    internal class MemberExpression<TSource, TDestination, TMember> : IMemberExpression<TSource, TDestination, TMember>
    {
        private readonly MemberOption memberOption;

        public PropertyInfo DestinationMember { get; }

        public MemberExpression(PropertyInfo property, MemberOption memberOption)
        {
            DestinationMember = property;
            this.memberOption = memberOption;
        }

        //--------------------------------------------------------------------------------
        // Ignore
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> Ignore()
        {
            memberOption.SetIgnore();
            return this;
        }

        //--------------------------------------------------------------------------------
        // Nested
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> Nested()
        {
            memberOption.SetNested();
            return this;
        }

        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> Order(int order)
        {
            memberOption.SetOrder(order);
            return this;
        }

        //--------------------------------------------------------------------------------
        // Condition
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, bool> condition)
        {
            memberOption.SetCondition(condition);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, ResolutionContext, bool> condition)
        {
            memberOption.SetCondition(condition);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, bool> condition)
        {
            memberOption.SetCondition(condition);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> Condition(Func<TSource, TDestination, ResolutionContext, bool> condition)
        {
            memberOption.SetCondition(condition);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> Condition(IMemberCondition<TSource, TDestination> condition)
        {
            memberOption.SetCondition(condition);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> Condition<TMemberCondition>()
            where TMemberCondition : IMemberCondition<TSource, TDestination>
        {
            memberOption.SetCondition<TMemberCondition>();
            return this;
        }

        //--------------------------------------------------------------------------------
        // MapFrom
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> expression)
        {
            memberOption.SetMapFrom(expression);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, ResolutionContext, TSourceMember>> expression)
        {
            memberOption.SetMapFrom(expression);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom(IValueResolver<TSource, TDestination, TMember> resolver)
        {
            memberOption.SetMapFrom(resolver);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember>
        {
            memberOption.SetMapFrom<TSource, TDestination, TMember, TValueResolver>();
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom(string sourcePath)
        {
            memberOption.SetMapFrom(sourcePath);
            return this;
        }

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(Func<TSourceMember, TMember> converter)
        {
            memberOption.SetConverter(converter);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(Func<TSourceMember, ResolutionContext, TMember> converter)
        {
            memberOption.SetConverter(converter);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember>(IValueConverter<TSourceMember, TMember> converter)
        {
            memberOption.SetConverter(converter);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> ConvertUsing<TSourceMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TMember>
        {
            memberOption.SetConverter<TSourceMember, TMember, TValueConverter>();
            return this;
        }

        //--------------------------------------------------------------------------------
        // Const
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> Const(TMember value)
        {
            memberOption.SetConstValue(value);
            return this;
        }

        //--------------------------------------------------------------------------------
        // NullIf
        //--------------------------------------------------------------------------------

        public IMemberExpression<TSource, TDestination, TMember> NullIf(TMember value)
        {
            memberOption.SetNullIfValue(value);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> NullIgnore()
        {
            memberOption.SetNullIgnore();
            return this;
        }
    }
}
