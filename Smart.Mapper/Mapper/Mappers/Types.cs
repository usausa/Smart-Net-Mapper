namespace Smart.Mapper.Mappers
{
    public enum FactoryType
    {
        None,
        FuncDestination,
        FuncSourceDestination,
        FuncSourceContextDestination,
        Interface,
        InterfaceType
    }

    public enum ActionType
    {
        None,
        Action,
        ActionContext,
        Interface,
        InterfaceType
    }

    public enum ConditionType
    {
        None,
        FuncSource,
        FuncSourceContext,
        FuncSourceDestinationContext,
        Interface,
        InterfaceType
    }

    public enum FromType
    {
        None,
        Property,
        Expression,
        ExpressionContext,
        Interface,
        InterfaceType,
        Path
    }

    public enum ConverterType
    {
        None,
        FuncSource,
        FuncSourceContext,
        Interface,
        InterfaceType
    }
}
