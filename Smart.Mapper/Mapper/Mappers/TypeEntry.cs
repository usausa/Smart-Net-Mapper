namespace Smart.Mapper.Mappers
{
    using System;

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
}
