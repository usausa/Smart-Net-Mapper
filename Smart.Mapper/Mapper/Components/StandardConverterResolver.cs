namespace Smart.Mapper.Components
{
    using System;

    using Smart.Converter;

    public sealed class StandardConverterResolver : IConverterResolver
    {
        private readonly IObjectConverter objectConverter;

        public StandardConverterResolver(IObjectConverter objectConverter)
        {
            this.objectConverter = objectConverter;
        }

        public Func<TSource, TDestination> Resolve<TSource, TDestination>()
        {
            // [MEMO] Bad performance boxed version.
            var converter = objectConverter.CreateConverter(typeof(TSource), typeof(TDestination));
            return x => (TDestination)converter(x);
        }
    }
}
