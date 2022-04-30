namespace Smart.Mapper.Mappers;

internal sealed class TypeEntry<T>
    where T : Enum
{
    public T Type { get; }

    public object Value { get; }

    public TypeEntry(T type, object value)
    {
        Type = type;
        Value = value;
    }
}

public sealed class ConverterEntry
{
    public ConverterType Type { get; }

    public Type SourceType { get; }

    public Type DestinationType { get; }

    public object Value { get; }

    public ConverterEntry(ConverterType type, Type sourceType, Type destinationType, object value)
    {
        Type = type;
        SourceType = sourceType;
        DestinationType = destinationType;
        Value = value;
    }
}

internal sealed class FromTypeEntry
{
    public FromType Type { get; }

    public Type MemberType { get; }

    public object Value { get; }

    public FromTypeEntry(FromType type, Type memberType, object value)
    {
        Type = type;
        MemberType = memberType;
        Value = value;
    }
}
