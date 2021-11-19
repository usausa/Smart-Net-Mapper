namespace Smart.Mapper.Rules;

using System;
using System.Reflection;

using Smart.Mapper.Options;

public sealed class IgnoreRule : IMappingRule
{
    private readonly Func<MemberInfo, bool> predicate;

    public IgnoreRule(Func<MemberInfo, bool> predicate)
    {
        this.predicate = predicate;
    }

    public void EditMapping(MappingOption option)
    {
    }

    public void EditMember(MemberOption option)
    {
        if (predicate(option.Member))
        {
            option.SetIgnore();
        }
    }
}
