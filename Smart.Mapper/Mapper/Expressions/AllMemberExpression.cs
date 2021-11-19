namespace Smart.Mapper.Expressions;

using System.Reflection;

using Smart.Mapper.Options;

public sealed class AllMemberExpression : IAllMemberExpression
{
    private readonly MemberOption memberOption;

    public MemberInfo DestinationMember => memberOption.Member;

    public AllMemberExpression(MemberOption memberOption)
    {
        this.memberOption = memberOption;
    }

    public IAllMemberExpression Ignore()
    {
        memberOption.SetIgnore();
        return this;
    }

    public IAllMemberExpression Nested()
    {
        memberOption.SetNested(null);
        return this;
    }

    public IAllMemberExpression Nested(string profile)
    {
        memberOption.SetNested(profile);
        return this;
    }

    public IAllMemberExpression Order(int order)
    {
        memberOption.SetOrder(order);
        return this;
    }
}
