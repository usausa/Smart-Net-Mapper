namespace Smart.Mapper.Expressions
{
    using System.Reflection;

    using Smart.Mapper.Options;

    public sealed class AllMemberExpression : IAllMemberExpression
    {
        private readonly MemberOption memberOption;

        public PropertyInfo DestinationMember => memberOption.Property;

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
            memberOption.SetNested();
            return this;
        }

        public IAllMemberExpression Order(int order)
        {
            memberOption.SetOrder(order);
            return this;
        }

        public IAllMemberExpression NullIgnore()
        {
            memberOption.SetNullIgnore();
            return this;
        }
    }
}
