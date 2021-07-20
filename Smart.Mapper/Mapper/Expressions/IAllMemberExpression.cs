namespace Smart.Mapper.Expressions
{
    using System.Reflection;

    public interface IAllMemberExpression
    {
        //--------------------------------------------------------------------------------
        // Info
        //--------------------------------------------------------------------------------

        PropertyInfo DestinationMember { get; }

        //--------------------------------------------------------------------------------
        // Ignore
        //--------------------------------------------------------------------------------

        IAllMemberExpression Ignore();

        //--------------------------------------------------------------------------------
        // Nest
        //--------------------------------------------------------------------------------

        // TODO IAllMemberExpression Nested();

        //--------------------------------------------------------------------------------
        // Order
        //--------------------------------------------------------------------------------

        IAllMemberExpression Order(int order);

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        // TODO IAllMemberExpression NullIgnore();
    }
}
