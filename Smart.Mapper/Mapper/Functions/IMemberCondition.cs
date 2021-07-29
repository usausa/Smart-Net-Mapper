namespace Smart.Mapper.Functions
{
    public interface IMemberCondition<in TSource, in TDestination>
    {
        bool Eval(TSource source, TDestination destination, ResolutionContext context);
    }
}
