namespace Smart.Mapper;

using Smart.Mapper.Options;

internal sealed class MapperEntry
{
    public string? Profile { get; }

    public MappingOption Option { get; }

    public MapperEntry(string? profile, MappingOption option)
    {
        Profile = profile;
        Option = option;
    }
}
