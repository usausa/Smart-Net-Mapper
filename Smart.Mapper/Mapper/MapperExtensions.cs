namespace Smart.Mapper
{
    using Smart.Mapper.Handlers;

    public static class MapperExtensions
    {
        //--------------------------------------------------------------------------------
        // Config
        //--------------------------------------------------------------------------------

        public static ObjectMapper ToMapper(this MapperConfig config) => new(config);

        public static MapperConfig AddDefaultMapper(this MapperConfig config)
        {
            config.Configure(c =>
            {
                c.Add<IMissingHandler, DefaultMapperHandler>();
            });

            return config;
        }
    }
}
