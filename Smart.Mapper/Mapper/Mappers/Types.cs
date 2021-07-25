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

    public enum MapType
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

    public enum ResolverType
    {
        None,
        Property,
        FuncSource,
        FuncSourceContext,
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
