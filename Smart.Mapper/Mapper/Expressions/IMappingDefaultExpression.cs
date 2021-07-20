namespace Smart.Mapper.Expressions
{
    using System;

    using Smart.Mapper.Functions;

    public interface IMappingDefaultExpression
    {
        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        // TODO
        //IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> converter);

        //IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember, ResolutionContext> converter);

        //IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> converter);

        //IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember, TValueConverter>()
        //    where TValueConverter : IValueConverter<TSourceMember, TDestinationMember>;

        //--------------------------------------------------------------------------------
        // Constant
        //--------------------------------------------------------------------------------

        // TODO IMappingDefaultExpression Const<TMember>(TMember value);

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        // TODO IMappingDefaultExpression NullIf<TMember>(TMember value);

        // TODO IMappingDefaultExpression NullIgnore(Type type);
    }
}
