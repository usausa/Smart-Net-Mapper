namespace Smart.Mapper.Functions
{
    public interface IMemberCondition<in TSourceMember, in TDestinationMember>
    {
        bool Eval(TSourceMember source, TDestinationMember destination, ResolutionContext context);
    }
}
