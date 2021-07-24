namespace Smart.Mapper
{
    public sealed class ResolutionContext
    {
        public object? Parameter { get; }

        public INestedMapper Mapper { get; }

        public ResolutionContext(object? parameter, INestedMapper mapper)
        {
            Parameter = parameter;
            Mapper = mapper;
        }
    }
}
