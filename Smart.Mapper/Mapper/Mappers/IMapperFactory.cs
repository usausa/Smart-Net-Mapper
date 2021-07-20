namespace Smart.Mapper.Mappers
{
    using Smart.Mapper.Options;

    internal interface IMapperFactory
    {
        object Create(MapperCreateContext context);
    }
}
