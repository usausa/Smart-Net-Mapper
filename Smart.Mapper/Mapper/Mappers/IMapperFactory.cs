namespace Smart.Mapper.Mappers
{
    internal interface IMapperFactory
    {
        object Create(MapperCreateContext context);
    }
}
