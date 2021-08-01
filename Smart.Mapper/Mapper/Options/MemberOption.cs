namespace Smart.Mapper.Options
{
    using System;
    using System.Collections.Generic;
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

        private FromTypeEntry? mapFrom;

        private bool useConst;
        private object? constValue;

        private bool useNullIf;
        private object? nullIfValue;

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
                if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type))
                {
                    mapFrom = new FromTypeEntry(FromType.Properties, pi.PropertyType, new[] { pi });
                    return;
                }
            }

            //if (value.Body is ConstantExpression constantExpression)
            //{
            //    useConst = true;
            //    constValue = constantExpression.Value;
            //    return;
            //}

            mapFrom = new FromTypeEntry(FromType.LazyFunc, typeof(TSourceMember), new Lazy<Func<TSource, TSourceMember>>(value.Compile));
        }

        public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, TSourceMember> func) =>
            mapFrom = new FromTypeEntry(FromType.Func, typeof(TSourceMember), func);

        public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func) =>
            mapFrom = new FromTypeEntry(FromType.FuncContext, typeof(TSourceMember), func);

        public void SetMapFrom<TSource, TDestination, TMember>(IValueResolver<TSource, TDestination, TMember> value) =>
            mapFrom = new FromTypeEntry(FromType.Interface, typeof(TMember), value);

        public void SetMapFrom<TSource, TDestination, TMember, TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember> =>
            mapFrom = new FromTypeEntry(FromType.InterfaceType, typeof(TMember), typeof(TValueResolver));

        public void SetMapFrom<TSource>(string sourcePath)
        {
            var type = typeof(TSource);
            var properties = new List<PropertyInfo>();
            foreach (var name in sourcePath.Split("."))
            {
                var pi = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (pi is null)
                {
                    throw new ArgumentException("Invalid source.", nameof(sourcePath));
                }

                properties.Add(pi);
                type = pi.PropertyType;
            }

            mapFrom = new FromTypeEntry(FromType.Properties, properties[^1].PropertyType, properties);
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

        internal int GetOrder() => order;

        internal TypeEntry<ConditionType>? GetCondition() => condition;

        internal FromTypeEntry? GetMapFrom() => mapFrom;

        internal bool UseConst() => useConst;

        internal object? GetConstValue() => constValue;

        internal bool UseNullIf() => useNullIf;

        internal object? GetNullIfValue() => nullIfValue;
    }
}
