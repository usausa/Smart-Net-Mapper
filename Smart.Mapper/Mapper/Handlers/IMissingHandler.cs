namespace Smart.Mapper.Handlers;

using Smart.Mapper.Options;

public interface IMissingHandler
{
    int Priority { get; }

    MappingOption? Handle(Type sourceType, Type destinationType);
}
