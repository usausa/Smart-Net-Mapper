namespace Smart.Mapper.Rules;

using System;
using System.Reflection;

using Smart.Mapper.Options;

public sealed class NestedRule : IMappingRule
{
    private readonly Func<MemberInfo, bool> predicate;

    private readonly string? profile;

    public NestedRule(Func<MemberInfo, bool> predicate, string? profile)
    {
        this.predicate = predicate;
        this.profile = profile;
    }

    public void EditMapping(MappingOption option)
    {
    }

    public void EditMember(MemberOption option)
    {
        if (predicate(option.Member))
        {
            option.SetNested(profile);
        }
    }
}
