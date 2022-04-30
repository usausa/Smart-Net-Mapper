namespace Smart.Mapper.Helpers;

using System.Linq.Expressions;
using System.Reflection;

internal static class ExpressionHelper
{
    public static MemberInfo? GetMemberInfo<TSource, TMember>(Expression<Func<TSource, TMember>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member;
        }

        return null;
    }
}
