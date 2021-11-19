namespace Smart.Mapper.Functions;

public interface IValueProvider<in TSource, in TDestination, out TMember>
{
    TMember Provide(TSource source, TDestination destination, ResolutionContext context);
}
