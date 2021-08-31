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
        Properties,
        LazyFunc,
        Func,
        FuncContext,
        Interface,
        InterfaceType
    }

    public enum ConverterType
    {
        None,
        FuncSource,
        FuncSourceContext,
        Interface,
        InterfaceType
    }

    public static class TypeExtensions
    {
        // Context

        public static bool HasContext(this FactoryType type) =>
            type == FactoryType.FuncSourceContextDestination ||
            type == FactoryType.Interface ||
            type == FactoryType.InterfaceType;

        public static bool HasContext(this ActionType type) =>
            type == ActionType.ActionContext ||
            type == ActionType.Interface ||
            type == ActionType.InterfaceType;

        public static bool HasContext(this ConditionType type) =>
            type == ConditionType.FuncSourceContext ||
            type == ConditionType.FuncSourceDestinationContext ||
            type == ConditionType.Interface ||
            type == ConditionType.InterfaceType;

        public static bool HasContext(this FromType type) =>
            type == FromType.FuncContext ||
            type == FromType.Interface ||
            type == FromType.InterfaceType;

        public static bool HasContext(this ConverterType type) =>
            type == ConverterType.FuncSourceContext ||
            type == ConverterType.Interface ||
            type == ConverterType.InterfaceType;

        // Destination parameter

        public static bool HasDestinationParameter(this ActionType type) =>
            type == ActionType.Action ||
            type == ActionType.ActionContext ||
            type == ActionType.Interface ||
            type == ActionType.InterfaceType;

        public static bool HasDestinationParameter(this ConditionType type) =>
            type == ConditionType.FuncSourceDestinationContext ||
            type == ConditionType.Interface ||
            type == ConditionType.InterfaceType;

        public static bool HasDestinationParameter(this FromType type) =>
            type == FromType.Interface ||
            type == FromType.InterfaceType;
    }
}
