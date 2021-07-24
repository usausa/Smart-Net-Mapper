namespace Smart.Mapper.Options
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Smart.Mapper.Functions;

    public class MappingOption
    {
        public Type SourceType { get; }

        public Type DestinationType { get; }

        // Mapping

        private bool factoryUseServiceProvider;

        private object? factory;

        private List<object>? beforeMaps;

        private List<object>? afterMaps;

        private Func<string, string?>? matcher;

        // Default

        private Dictionary<Tuple<Type, Type>, object>? converters;

        private Dictionary<Type, object?>? constValues;

        private Dictionary<Type, object?>? nullIfValues;

        private HashSet<Type>? nullIgnores;

        // Member

        public IReadOnlyList<MemberOption> MemberOptions { get; }

        public MappingOption(Type sourceType, Type destinationType)
        {
            SourceType = sourceType;
            DestinationType = destinationType;
            MemberOptions = destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.CanWrite)
                .Select(x => new MemberOption(x))
                .ToArray();
        }

        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        public void SetFactoryUseServiceProvider() => factoryUseServiceProvider = true;

        public void SetFactory<TDestination>(Func<TDestination> value) => factory = value;

        public void SetFactory<TSource, TDestination>(Func<TSource, TDestination> value) => factory = value;

        public void SetFactory<TSource, TDestination>(Func<TSource, ResolutionContext, TDestination> value) => factory = value;

        public void SetFactory<TSource, TDestination>(IObjectFactory<TSource, TDestination> value) => factory = value;

        public void SetFactory<TSource, TDestination, TObjectFactory>()
            where TObjectFactory : IObjectFactory<TSource, TDestination> => factory = typeof(TObjectFactory);

        //--------------------------------------------------------------------------------
        // Pre/Post process
        //--------------------------------------------------------------------------------

        public void AddBeforeMap<TSource, TDestination>(Action<TSource, TDestination> action) =>
            AddBeforeMapInternal(action);

        public void AddBeforeMap<TSource, TDestination>(Action<TSource, TDestination, ResolutionContext> action) =>
            AddBeforeMapInternal(action);

        public void AddBeforeMap<TSource, TDestination>(IMappingAction<TSource, TDestination> action) =>
            AddBeforeMapInternal(action);

        public void AddBeforeMap<TSource, TDestination, TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination> =>
            AddBeforeMapInternal(typeof(TMappingAction));

        private void AddBeforeMapInternal(object value)
        {
            beforeMaps ??= new List<object>();
            beforeMaps.Add(value);
        }

        public void AddAfterMap<TSource, TDestination>(Action<TSource, TDestination> action) =>
            AddAfterMapInternal(action);

        public void AddAfterMap<TSource, TDestination>(Action<TSource, TDestination, ResolutionContext> action) =>
            AddAfterMapInternal(action);

        public void AddAfterMap<TSource, TDestination>(IMappingAction<TSource, TDestination> action) =>
            AddAfterMapInternal(action);

        public void AddAfterMap<TSource, TDestination, TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination> =>
            AddAfterMapInternal(typeof(TMappingAction));

        private void AddAfterMapInternal(object value)
        {
            afterMaps ??= new List<object>();
            afterMaps.Add(value);
        }

        //--------------------------------------------------------------------------------
        // Match
        //--------------------------------------------------------------------------------

        public void SetMatcher(Func<string, string?> value) => matcher = value;

        //--------------------------------------------------------------------------------
        // Converter
        //--------------------------------------------------------------------------------

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(new Tuple<Type, Type>(typeof(TSourceMember), typeof(TDestinationMember)), value);

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember, ResolutionContext> value) =>
            SetConverterInternal(new Tuple<Type, Type>(typeof(TSourceMember), typeof(TDestinationMember)), value);

        public void SetConverter<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(new Tuple<Type, Type>(typeof(TSourceMember), typeof(TDestinationMember)), value);

        public void SetConverter<TSourceMember, TDestinationMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TDestinationMember> =>
            SetConverterInternal(new Tuple<Type, Type>(typeof(TSourceMember), typeof(TDestinationMember)), typeof(TValueConverter));

        private void SetConverterInternal(Tuple<Type, Type> pair, object value)
        {
            converters ??= new Dictionary<Tuple<Type, Type>, object>();
            converters[pair] = value;
        }

        //--------------------------------------------------------------------------------
        // Constant
        //--------------------------------------------------------------------------------

        public void SetConstValue<TMember>(TMember value)
        {
            constValues ??= new Dictionary<Type, object?>();
            constValues[typeof(TMember)] = value;
        }

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        public void SetNullIfValue<TMember>(TMember value)
        {
            nullIfValues ??= new Dictionary<Type, object?>();
            nullIfValues[typeof(TMember)] = value;
        }

        public void SetNullIgnore(Type type)
        {
            nullIgnores ??= new HashSet<Type>();
            nullIgnores.Add(type);
        }

        //--------------------------------------------------------------------------------
        // Internal
        //--------------------------------------------------------------------------------

        internal bool IsFactoryUseServiceProvider() => factoryUseServiceProvider;

        internal object? GetFactory() => factory;

        internal IReadOnlyList<object> GetBeforeMaps() => beforeMaps ?? (IReadOnlyList<object>)Array.Empty<object>();

        internal IReadOnlyList<object> GetAfterMaps() => afterMaps ?? (IReadOnlyList<object>)Array.Empty<object>();

        internal Func<string, string?>? GetMatcher() => matcher;

        // Default

        internal bool TryGetConverter(Tuple<Type, Type> pair, [NotNullWhen(true)] out object? value)
        {
            if ((converters is not null) && converters.TryGetValue(pair, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        internal bool TryGetConstValue(Type type, out object? value)
        {
            if ((constValues is not null) && constValues.TryGetValue(type, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        internal bool TryGetNullIfValue(Type type, out object? value)
        {
            if ((nullIfValues is not null) && nullIfValues.TryGetValue(type, out value))
            {
                return true;
            }

            value = null;
            return false;
        }

        internal bool IsNullIgnore(Type type)
        {
            return (nullIgnores is not null) && nullIgnores.Contains(type);
        }
    }
}
