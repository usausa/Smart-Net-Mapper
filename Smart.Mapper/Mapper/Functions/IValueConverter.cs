namespace Smart.Mapper.Functions
{
    public interface IValueConverter<in TSourceMember, out TDestinationMember>
    {
        TDestinationMember Convert(TSourceMember value, ResolutionContext context);
    }
}
