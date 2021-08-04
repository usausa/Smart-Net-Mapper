namespace Smart.Mapper.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Smart.Mapper.Mappers;

    internal sealed class ConverterRepository
    {
        private readonly Dictionary<Tuple<Type, Type>, TypeEntry<ConverterType>> converters;

        public ConverterRepository()
        {
            converters = new Dictionary<Tuple<Type, Type>, TypeEntry<ConverterType>>();
        }

        public ConverterRepository(Dictionary<Tuple<Type, Type>, TypeEntry<ConverterType>> converters)
        {
            this.converters = new Dictionary<Tuple<Type, Type>, TypeEntry<ConverterType>>(converters);
        }

        public void Set(Type sourceType, Type destinationType, TypeEntry<ConverterType> func)
        {
            converters[Tuple.Create(sourceType, destinationType)] = func;
        }

        public bool TryGetConverter(Type sourceType, Type destinationType, [NotNullWhen(true)] out TypeEntry<ConverterType>? value)
        {
            if (converters.TryGetValue(Tuple.Create(sourceType, destinationType), out value))
            {
                return true;
            }

            var sourceUnderlyingType = Nullable.GetUnderlyingType(sourceType);
            if (sourceUnderlyingType is not null)
            {
                if (converters.TryGetValue(Tuple.Create(sourceUnderlyingType, destinationType), out value))
                {
                    return true;
                }

                sourceType = sourceUnderlyingType;
            }

            var destinationUnderlyingType = Nullable.GetUnderlyingType(destinationType);
            if (destinationUnderlyingType is not null)
            {
                if (converters.TryGetValue(Tuple.Create(sourceType, destinationUnderlyingType), out value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
