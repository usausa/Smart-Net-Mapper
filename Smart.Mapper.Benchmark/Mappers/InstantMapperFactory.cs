namespace Smart.Mapper.Benchmark.Mappers;

using System.Reflection;

using Smart.Converter;
using Smart.Reflection;

public static class InstantMapperFactory
{
    public static IActionMapper<TSource, TDestination> Create<TSource, TDestination>()
    {
        var sourceType = typeof(TSource);
        var destinationType = typeof(TDestination);

        var factory = DelegateFactory.Default.CreateFactory<TDestination>();
        var actions = new List<Action<TSource, TDestination>>();

        var destinationProperties = destinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static x => x.Name, static x => x, StringComparer.OrdinalIgnoreCase);
        foreach (var sourcePi in sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!destinationProperties.TryGetValue(sourcePi.Name, out var destinationPi))
            {
                continue;
            }

            if (!sourcePi.CanRead || !destinationPi.CanWrite)
            {
                continue;
            }

            var getter = DelegateFactory.Default.CreateGetter(sourcePi);
            if (getter is null)
            {
                continue;
            }

            var setter = DelegateFactory.Default.CreateSetter(destinationPi);
            if (setter is null)
            {
                continue;
            }

            if (sourcePi.PropertyType.IsAssignableFrom(destinationPi.PropertyType))
            {
                actions.Add((s, d) => setter(d, getter(s)));
            }
            else
            {
                var converter = ObjectConverter.Default.CreateConverter(sourcePi.PropertyType, destinationPi.PropertyType);
                if (converter is not null)
                {
                    actions.Add((s, d) => setter(d, converter(getter(s))));
                }
            }
        }

        return new ActionMapper<TSource, TDestination>(
            factory,
            [.. actions]);
    }
}
