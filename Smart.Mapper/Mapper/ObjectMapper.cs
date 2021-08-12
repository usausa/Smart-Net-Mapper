namespace Smart.Mapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Smart.ComponentModel;

    using Smart.Mapper.Handlers;
    using Smart.Mapper.Helpers;
    using Smart.Mapper.Mappers;
    using Smart.Mapper.Options;

    public sealed class ObjectMapper : DisposableObject
    {
        private readonly object sync = new();

        private readonly ComponentContainer components;

        private readonly IMissingHandler[] handlers;

        private readonly IMapperFactory factory;

        private readonly DefaultOption defaultOption;

        private readonly Dictionary<(string?, Type, Type), MappingOption> mapperOptions;

        private readonly MapperHashArray mapperCache = new(128);

        private readonly ProfileMapperHashArray profileMapperCache = new(32);

        private readonly NestedMapper nestedMapper;

        private readonly Dictionary<string, ProfileNestedMapper> profileNestedMappers = new();

        internal ObjectMapper(MapperConfig config)
        {
            components = config.GetComponentContainer();
            defaultOption = config.GetDefaultOption();
            mapperOptions = config.GetEntries().ToDictionary(
                x => (x.Profile, x.Option.SourceType, x.Option.DestinationType),
                x => x.Option);

            handlers = components.GetAll<IMissingHandler>().OrderByDescending(x => x.Priority).ToArray();
            factory = components.Get<IMapperFactory>();

            nestedMapper = new NestedMapper(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        private object CreateTypeInfo(string? profile, Type sourceType, Type destinationType)
        {
            lock (sync)
            {
                var mapperOption = FindMappingOption(profile, sourceType, destinationType);
                if (mapperOption is null)
                {
                    throw new InvalidOperationException(String.IsNullOrEmpty(profile)
                        ? $"Mapper not found. sourceType=[{sourceType}], destinationType=[{destinationType}]"
                        : $"Mapper not found. profile=[{profile}], sourceType=[{sourceType}], destinationType=[{destinationType}]");
                }

                if (String.IsNullOrEmpty(profile))
                {
                    return factory.Create(new MapperCreateContext(sourceType, destinationType, defaultOption, mapperOption, nestedMapper));
                }
                else
                {
                    if (!profileNestedMappers.TryGetValue(profile, out var profileNestedMapper))
                    {
                        profileNestedMapper = new ProfileNestedMapper(this, profile);
                        profileNestedMappers[profile] = profileNestedMapper;
                    }

                    return factory.Create(new MapperCreateContext(sourceType, destinationType, defaultOption, mapperOption, profileNestedMapper));
                }
            }
        }

        private MappingOption? FindMappingOption(string? profile, Type sourceType, Type destinationType)
        {
            if (mapperOptions.TryGetValue((profile, sourceType, destinationType), out var mapperOption))
            {
                return mapperOption;
            }

            var sourceNullableType = Nullable.GetUnderlyingType(sourceType);
            var destinationNullableType = Nullable.GetUnderlyingType(destinationType);

            if ((destinationNullableType is not null) &&
                mapperOptions.TryGetValue((profile, sourceType, destinationNullableType), out mapperOption))
            {
                return mapperOption;
            }

            if ((sourceNullableType is not null) &&
                mapperOptions.TryGetValue((profile, sourceNullableType, destinationType), out mapperOption))
            {
                return mapperOption;
            }

            if ((sourceNullableType is not null) &&
                (destinationNullableType is not null) &&
                mapperOptions.TryGetValue((profile, sourceNullableType, destinationNullableType), out mapperOption))
            {
                return mapperOption;
            }

            return handlers
                .Select(x => x.Handle(sourceType, destinationType))
                .FirstOrDefault(x => x is not null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapperInfo<TSource, TDestination> FindTypeInfo<TSource, TDestination>()
        {
            if (!mapperCache.TryGetValue(typeof(TSource), typeof(TDestination), out var info))
            {
                info = mapperCache.AddIfNotExist(typeof(TSource), typeof(TDestination), (ts, td) => CreateTypeInfo(null, ts, td));
            }

            return (MapperInfo<TSource, TDestination>)info;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapperInfo<TSource, TDestination> FindTypeInfo<TSource, TDestination>(string profile)
        {
            if (!profileMapperCache.TryGetValue(profile, typeof(TSource), typeof(TDestination), out var info))
            {
                // ReSharper disable once ConvertClosureToMethodGroup
                info = profileMapperCache.AddIfNotExist(profile, typeof(TSource), typeof(TDestination), (p, ts, td) => CreateTypeInfo(p, ts, td));
            }

            return (MapperInfo<TSource, TDestination>)info;
        }

        //--------------------------------------------------------------------------------
        // Get
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>() =>
            FindTypeInfo<TSource, TDestination>().MapAction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Action<TSource, TDestination, object> GetParameterMapperAction<TSource, TDestination>() =>
            FindTypeInfo<TSource, TDestination>().ParameterMapAction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>() =>
            FindTypeInfo<TSource, TDestination>().MapFunc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<TSource, object, TDestination> GetParameterMapperFunc<TSource, TDestination>() =>
            FindTypeInfo<TSource, TDestination>().ParameterMapFunc;

        // With profile

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>(string profile) =>
            FindTypeInfo<TSource, TDestination>(profile).MapAction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Action<TSource, TDestination, object> GetParameterMapperAction<TSource, TDestination>(string profile) =>
            FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>(string profile) =>
            FindTypeInfo<TSource, TDestination>(profile).MapFunc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Func<TSource, object, TDestination> GetParameterMapperFunc<TSource, TDestination>(string profile) =>
            FindTypeInfo<TSource, TDestination>(profile).ParameterMapFunc;

        //--------------------------------------------------------------------------------
        // Map
        //--------------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination Map<TSource, TDestination>(TSource source) =>
            FindTypeInfo<TSource, TDestination>().MapFunc(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
            FindTypeInfo<TSource, TDestination>().MapAction(source, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination MapAlso<TSource, TDestination>(TSource source, TDestination destination)
        {
            FindTypeInfo<TSource, TDestination>().MapAction(source, destination);
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination Map<TSource, TDestination>(TSource source, object? parameter) =>
            FindTypeInfo<TSource, TDestination>().ParameterMapFunc(source, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TSource, TDestination>(TSource source, TDestination destination, object? parameter) =>
            FindTypeInfo<TSource, TDestination>().ParameterMapAction(source, destination, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination MapAlso<TSource, TDestination>(TSource source, TDestination destination, object? parameter)
        {
            FindTypeInfo<TSource, TDestination>().ParameterMapAction(source, destination, parameter);
            return destination;
        }

        // With profile

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination Map<TSource, TDestination>(string profile, TSource source) =>
            FindTypeInfo<TSource, TDestination>(profile).MapFunc(source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TSource, TDestination>(string profile, TSource source, TDestination destination) =>
            FindTypeInfo<TSource, TDestination>(profile).MapAction(source, destination);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination MapAlso<TSource, TDestination>(string profile, TSource source, TDestination destination)
        {
            FindTypeInfo<TSource, TDestination>(profile).MapAction(source, destination);
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination Map<TSource, TDestination>(string profile, TSource source, object? parameter) =>
            FindTypeInfo<TSource, TDestination>(profile).ParameterMapFunc(source, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Map<TSource, TDestination>(string profile, TSource source, TDestination destination, object? parameter) =>
            FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction(source, destination, parameter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TDestination MapAlso<TSource, TDestination>(string profile, TSource source, TDestination destination, object? parameter)
        {
            FindTypeInfo<TSource, TDestination>(profile).ParameterMapAction(source, destination, parameter);
            return destination;
        }

        //--------------------------------------------------------------------------------
        // Nested mapper
        //--------------------------------------------------------------------------------

        private sealed class NestedMapper : INestedMapper
        {
            private readonly ObjectMapper mapper;

            public NestedMapper(ObjectMapper mapper)
            {
                this.mapper = mapper;
            }

            public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>() =>
                mapper.GetMapperAction<TSource, TDestination>();

            public Action<TSource, TDestination, object> GetParameterMapperAction<TSource, TDestination>() =>
                mapper.GetParameterMapperAction<TSource, TDestination>();

            public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>() =>
                mapper.GetMapperFunc<TSource, TDestination>();

            public Func<TSource, object, TDestination> GetParameterMapperFunc<TSource, TDestination>() =>
                mapper.GetParameterMapperFunc<TSource, TDestination>();

            public TDestination Map<TSource, TDestination>(TSource source) =>
                mapper.Map<TSource, TDestination>(source);

            public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
                mapper.Map(source, destination);

            public TDestination Map<TSource, TDestination>(TSource source, object? parameter) =>
                mapper.Map<TSource, TDestination>(source, parameter);

            public void Map<TSource, TDestination>(TSource source, TDestination destination, object? parameter) =>
                mapper.Map(source, destination, parameter);
        }

        private sealed class ProfileNestedMapper : INestedMapper
        {
            private readonly ObjectMapper mapper;

            private readonly string profile;

            public ProfileNestedMapper(ObjectMapper mapper, string profile)
            {
                this.mapper = mapper;
                this.profile = profile;
            }

            public Action<TSource, TDestination> GetMapperAction<TSource, TDestination>() =>
                mapper.GetMapperAction<TSource, TDestination>(profile);

            public Action<TSource, TDestination, object> GetParameterMapperAction<TSource, TDestination>() =>
                mapper.GetParameterMapperAction<TSource, TDestination>(profile);

            public Func<TSource, TDestination> GetMapperFunc<TSource, TDestination>() =>
                mapper.GetMapperFunc<TSource, TDestination>(profile);

            public Func<TSource, object, TDestination> GetParameterMapperFunc<TSource, TDestination>() =>
                mapper.GetParameterMapperFunc<TSource, TDestination>(profile);

            public TDestination Map<TSource, TDestination>(TSource source) =>
                mapper.Map<TSource, TDestination>(profile, source);

            public void Map<TSource, TDestination>(TSource source, TDestination destination) =>
                mapper.Map(profile, source, destination);

            public TDestination Map<TSource, TDestination>(TSource source, object? parameter) =>
                mapper.Map<TSource, TDestination>(profile, source, parameter);

            public void Map<TSource, TDestination>(TSource source, TDestination destination, object? parameter) =>
                mapper.Map(profile, source, destination, parameter);
        }
    }
}
