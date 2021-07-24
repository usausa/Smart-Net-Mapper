namespace Smart.Mapper.Options
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    using Smart.Mapper.Functions;

    public class MemberOption
    {
        public PropertyInfo Property { get; }

        private bool ignore;

        private bool nested;

        private int order = Int32.MaxValue;

        private object? condition;

        private object? mapFrom;

        private object? converter;

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

        public void SetCondition<TSource>(Func<TSource, bool> value) => condition = value;

        public void SetCondition<TSource>(Func<TSource, ResolutionContext, bool> value) => condition = value;

        public void SetCondition<TSource, TDestination>(Func<TSource, TDestination, bool> value) => condition = value;

        public void SetCondition<TSource, TDestination>(Func<TSource, TDestination, ResolutionContext, bool> value) => condition = value;

        public void SetCondition<TSource, TDestination>(IMemberCondition<TSource, TDestination> value) => condition = value;

        public void SetCondition<TMemberCondition>() => condition = typeof(TMemberCondition);

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
                    mapFrom = pi;
                    return;
                }
            }

            if (value.Body is ConstantExpression constantExpression)
            {
                useConst = true;
                constValue = constantExpression.Value;
                return;
            }

            mapFrom = value.Compile();
        }

        public void SetMapFrom<TSource, TSourceMember>(Expression<Func<TSource, ResolutionContext, TSourceMember>> value)
        {
            if (value.Body is MemberExpression memberExpression)
            {
                var type = typeof(TSource);
                if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type) && type.IsSubclassOf(pi.ReflectedType))
                {
                    mapFrom = pi;
                    return;
                }
            }

            if (value.Body is ConstantExpression constantExpression)
            {
                useConst = true;
                constValue = constantExpression.Value;
                return;
            }

            mapFrom = value.Compile();
        }

        public void SetMapFrom<TSource, TDestination, TMember>(IValueResolver<TSource, TDestination, TMember> value) => mapFrom = value;

        public void SetMapFrom<TSource, TDestination, TMember, TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember> => mapFrom = typeof(TValueResolver);

        public void SetMapFrom(string value) => mapFrom = value;

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        public void SetConverter<TSourceMember, TMember>(Func<TSourceMember, TMember> value) => converter = value;

        public void SetConverter<TSourceMember, TMember>(Func<TSourceMember, ResolutionContext, TMember> value) => converter = value;

        public void SetConverter<TSourceMember, TMember>(IValueConverter<TSourceMember, TMember> value) => converter = value;

        public void SetConverter<TSourceMember, TMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TMember> => converter = typeof(TValueConverter);

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

        internal object? GetCondition() => condition;

        internal object? GetMapFrom() => mapFrom;

        internal object? GetConverter() => converter;

        internal bool UseConst() => useConst;

        internal object? GetConstValue() => constValue;

        internal bool UseNullIf() => useNullIf;

        internal object? GetNullIfValue() => nullIfValue;

        internal bool IsNullIgnore() => nullIgnore;
    }
}
