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
}
