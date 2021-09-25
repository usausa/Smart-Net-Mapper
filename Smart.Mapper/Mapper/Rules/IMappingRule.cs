namespace Smart.Mapper.Rules
{
    using Smart.Mapper.Options;

    public interface IMappingRule
    {
        void EditMapping(MappingOption option);

        void EditMember(MemberOption option);
    }
}
