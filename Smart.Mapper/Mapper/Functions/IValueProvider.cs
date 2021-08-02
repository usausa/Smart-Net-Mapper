namespace Smart.Mapper.Functions
{
    public interface IValueProvider<in TSource, in TDestination, out TMember>
    {
        TMember Resolve(TSource source, TDestination destination, ResolutionContext context);
    }
}
