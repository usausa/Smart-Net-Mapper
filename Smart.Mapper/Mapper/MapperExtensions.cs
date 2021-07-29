namespace Smart.Mapper
{
    using System;

    using Smart.Mapper.Handlers;

    public static class MapperExtensions
    {
        //--------------------------------------------------------------------------------
        // Config
        //--------------------------------------------------------------------------------

        public static ObjectMapper ToMapper(this MapperConfig config) => new(config);

        public static MapperConfig UseServiceProvider<TServiceProvider>(this MapperConfig config)
            where TServiceProvider : IServiceProvider
        {
            config.Configure(c => c.Add<IServiceProvider, TServiceProvider>());
            return config;
        }

        public static MapperConfig UseServiceProvider(this MapperConfig config, IServiceProvider serviceProvider)
        {
            config.Configure(c => c.Add(serviceProvider));
            return config;
        }

        public static MapperConfig AddMissingHandler<TMissingHandler>(this MapperConfig config)
            where TMissingHandler : IMissingHandler
        {
            config.Configure(c => c.Add<IMissingHandler, TMissingHandler>());
            return config;
        }

        public static MapperConfig AddMissingHandler(this MapperConfig config, IMissingHandler missingHandler)
        {
            config.Configure(c => c.Add(missingHandler));
            return config;
        }

        public static MapperConfig AddDefaultMapper(this MapperConfig config) =>
            config.AddMissingHandler<DefaultMapperHandler>();
    }
}
