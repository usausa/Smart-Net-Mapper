namespace Smart.Mapper.Mappers
{
    using Smart.Mapper.Options;

    public sealed class MapperCreateContext
    {
        public DefaultOption DefaultOption { get; }

        public MappingOption MappingOption { get; }

        public MapperCreateContext(
            DefaultOption defaultOption,
            MappingOption mappingOption)
        {
            DefaultOption = defaultOption;
            MappingOption = mappingOption;
        }

        // TODO Funcで？
        //object GetNestedMapper(Type sourceType, Type destinationType);
    }
}
