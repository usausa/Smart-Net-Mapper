namespace Smart.Mapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Smart.ComponentModel;

    using Smart.Mapper.Collections;
    using Smart.Mapper.Handlers;
    using Smart.Mapper.Mappers;
    using Smart.Mapper.Options;

    public sealed class ObjectMapper : DisposableObject
    {
        private readonly MapperHashArray mapperCache = new(128);
        private readonly ProfileMapperHashArray profileMapperCache = new(32);

        private readonly object sync = new();

        private readonly ComponentContainer components;

        private readonly DefaultOption defaultOption;

        private readonly Dictionary<(string?, Type, Type), MappingOption> mapperOptions;

        private readonly IMissingHandler[] handlers;

        private readonly IMapperFactory factory;

        internal ObjectMapper(MapperConfig config)
        {
            components = config.GetComponentContainer();
            defaultOption = config.GetDefaultOption();
            mapperOptions = config.GetEntries().ToDictionary(
                x => (x.Profile, x.Option.SourceType, x.Option.DestinationType),
                x => x.Option);

            handlers = components.GetAll<IMissingHandler>().OrderByDescending(x => x.Priority).ToArray();
            factory = components.Get<IMapperFactory>();
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
                if (!mapperOptions.TryGetValue((profile, sourceType, destinationType), out var mapperOption))
                {
                    mapperOption = handlers
                        .Select(x => x.Handle(sourceType, destinationType))
                        .FirstOrDefault(x => x is not null);
                }

                if (mapperOption is null)
                {
                    throw new InvalidOperationException(String.IsNullOrEmpty(profile)
                        ? $"Mapper not found. sourceType=[{sourceType}], destinationType=[{destinationType}]"
                        : $"Mapper not found. profile=[{profile}], sourceType=[{sourceType}], destinationType=[{destinationType}]");
                }

                // TODO
                return factory.Create(new MapperCreateContext(defaultOption, mapperOption, null!));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private MapperInfo<TSource, TDestination> FindTypeInfo<TSource, TDestination>()
        {
            if (!mapperCache.TryGetValue(typeof(TSource), typeof(TDestination), out var info))
            {
                info = mapperCache.AddIfNotExist(typeof(TSource), typeof(TDestination), (ts, td) => CreateTypeInfo(null, ts, td));
            }

            return (MapperInfo<TSource, TDestination>)info!;
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
    }
}
