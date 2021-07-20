namespace Smart.Mapper.Functions
{
    public interface IObjectFactory<in TSource, out TDestination>
    {
        TDestination Create(TSource source, ResolutionContext context);
    }
}
