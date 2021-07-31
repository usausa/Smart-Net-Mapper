namespace Smart.Mapper.Options
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    using Smart.Mapper.Functions;
    using Smart.Mapper.Mappers;

    public class MemberOption
    {
        public PropertyInfo Property { get; }

        private bool ignore;

        private bool nested;

        private int order = Int32.MaxValue;

        private TypeEntry<ConditionType>? condition;

        private TypeEntry<FromType>? mapFrom;

        private bool useConst;
        private object? constValue;

        private bool useNullIf;
        private object? nullIfValue;

        private bool nullIgnore;

        public MemberOption(PropertyInfo property)
        {
            Property = property;
        }

        //--------------------------------------------------------------------------------
        // Ignore
        //--------------------------------------------------------------------------------

        public void SetIgnore() => ignore = true;

        //--------------------------------------------------------------------------------
        // Nested
        //--------------------------------------------------------------------------------

        public void SetNested() => nested = true;

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
                if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type) && type.IsSubclassOf(pi.ReflectedType))
                {
                    mapFrom = new TypeEntry<FromType>(FromType.Property, pi);
                    return;
                }
            }

            if (value.Body is ConstantExpression constantExpression)
            {
                useConst = true;
                constValue = constantExpression.Value;
                return;
            }

            mapFrom = new TypeEntry<FromType>(FromType.Expression, new Lazy<Func<TSource, TSourceMember>>(value.Compile));
        }

        public void SetMapFrom<TSource, TSourceMember>(Expression<Func<TSource, ResolutionContext, TSourceMember>> value)
        {
            if (value.Body is MemberExpression memberExpression)
            {
                var type = typeof(TSource);
                if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type) && type.IsSubclassOf(pi.ReflectedType))
                {
                    mapFrom = new TypeEntry<FromType>(FromType.Property, pi);
                    return;
                }
            }

            if (value.Body is ConstantExpression constantExpression)
            {
                useConst = true;
                constValue = constantExpression.Value;
                return;
            }

            mapFrom = new TypeEntry<FromType>(FromType.ExpressionContext, new Lazy<Func<TSource, ResolutionContext, TSourceMember>>(value.Compile));
        }

        public void SetMapFrom<TSource, TDestination, TMember>(IValueResolver<TSource, TDestination, TMember> value) =>
            mapFrom = new TypeEntry<FromType>(FromType.Interface, value);

        public void SetMapFrom<TSource, TDestination, TMember, TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember> =>
            mapFrom = new TypeEntry<FromType>(FromType.InterfaceType, typeof(TValueResolver));

        public void SetMapFrom(string value) =>
            mapFrom = new TypeEntry<FromType>(FromType.Path, value);

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

        public void SetNullIgnore() => nullIgnore = true;

        //--------------------------------------------------------------------------------
        // Internal
        //--------------------------------------------------------------------------------

        internal bool IsIgnore() => ignore;

        internal bool IsNested() => nested;

        internal int GetOrder() => order;

        internal TypeEntry<ConditionType>? GetCondition() => condition;

        internal TypeEntry<FromType>? GetMapFrom() => mapFrom;

        internal bool UseConst() => useConst;

        internal object? GetConstValue() => constValue;

        internal bool UseNullIf() => useNullIf;

        internal object? GetNullIfValue() => nullIfValue;

        internal bool IsNullIgnore() => nullIgnore;
    }
}
