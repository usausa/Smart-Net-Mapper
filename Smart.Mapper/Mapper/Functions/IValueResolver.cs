namespace Smart.Mapper.Functions
{
    public interface IValueResolver<in TSource, in TDestination, out TDestinationMember>
    {
        TDestinationMember Resolve(TSource source, TDestination destination, ResolutionContext context);
    }
}
