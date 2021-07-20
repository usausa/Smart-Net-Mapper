namespace Smart.Mapper.Helpers
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    internal static class ExpressionHelper
    {
        public static PropertyInfo? GetPrpPropertyInfo<TSource, TMember>(Expression<Func<TSource, TMember>> expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member as PropertyInfo;
            }

            return null;
        }
    }
}
