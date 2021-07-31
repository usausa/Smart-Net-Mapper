namespace Smart.Mapper.Options
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Smart.Mapper.Functions;
    using Smart.Mapper.Helpers;
    using Smart.Mapper.Mappers;

    public class MappingOption
    {
        public Type SourceType { get; }

        public Type DestinationType { get; }

        // Mapping

        private bool factoryUseServiceProvider;

        private TypeEntry<FactoryType>? factory;

        private List<TypeEntry<ActionType>>? beforeMaps;

        private List<TypeEntry<ActionType>>? afterMaps;

        private Func<string, string?>? matcher;

        // Default

        private ConverterRepository? converters;

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

        public void SetFactory<TDestination>(Func<TDestination> value) =>
            factory = new TypeEntry<FactoryType>(FactoryType.FuncDestination, value);

        public void SetFactory<TSource, TDestination>(Func<TSource, TDestination> value) =>
            factory = new TypeEntry<FactoryType>(FactoryType.FuncSourceDestination, value);

        public void SetFactory<TSource, TDestination>(Func<TSource, ResolutionContext, TDestination> value) =>
            factory = new TypeEntry<FactoryType>(FactoryType.FuncSourceContextDestination, value);

        public void SetFactory<TSource, TDestination>(IObjectFactory<TSource, TDestination> value) =>
            factory = new TypeEntry<FactoryType>(FactoryType.Interface, value);

        public void SetFactory<TSource, TDestination, TObjectFactory>()
            where TObjectFactory : IObjectFactory<TSource, TDestination> =>
            factory = new TypeEntry<FactoryType>(FactoryType.InterfaceType, typeof(TObjectFactory));

        //--------------------------------------------------------------------------------
        // Pre/Post process
        //--------------------------------------------------------------------------------

        public void AddBeforeMap<TSource, TDestination>(Action<TSource, TDestination> action) =>
            AddBeforeMapInternal(ActionType.Action, action);

        public void AddBeforeMap<TSource, TDestination>(Action<TSource, TDestination, ResolutionContext> action) =>
            AddBeforeMapInternal(ActionType.ActionContext, action);

        public void AddBeforeMap<TSource, TDestination>(IMappingAction<TSource, TDestination> action) =>
            AddBeforeMapInternal(ActionType.Interface, action);

        public void AddBeforeMap<TSource, TDestination, TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination> =>
            AddBeforeMapInternal(ActionType.InterfaceType, typeof(TMappingAction));

        private void AddBeforeMapInternal(ActionType type, object value)
        {
            beforeMaps ??= new List<TypeEntry<ActionType>>();
            beforeMaps.Add(new TypeEntry<ActionType>(type, value));
        }

        public void AddAfterMap<TSource, TDestination>(Action<TSource, TDestination> action) =>
            AddAfterMapInternal(ActionType.Action, action);

        public void AddAfterMap<TSource, TDestination>(Action<TSource, TDestination, ResolutionContext> action) =>
            AddAfterMapInternal(ActionType.ActionContext, action);

        public void AddAfterMap<TSource, TDestination>(IMappingAction<TSource, TDestination> action) =>
            AddAfterMapInternal(ActionType.Interface, action);

        public void AddAfterMap<TSource, TDestination, TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination> =>
            AddAfterMapInternal(ActionType.InterfaceType, typeof(TMappingAction));

        private void AddAfterMapInternal(ActionType type, object value)
        {
            afterMaps ??= new List<TypeEntry<ActionType>>();
            afterMaps.Add(new TypeEntry<ActionType>(type, value));
        }

        //--------------------------------------------------------------------------------
        // Match
        //--------------------------------------------------------------------------------

        public void SetMatcher(Func<string, string?> value) => matcher = value;

        //--------------------------------------------------------------------------------
        // Converter
        //--------------------------------------------------------------------------------

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.FuncSource, value));

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember, ResolutionContext> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.FuncSourceContext, value));

        public void SetConverter<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.Interface, value));

        public void SetConverter<TSourceMember, TDestinationMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TDestinationMember> =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.InterfaceType, typeof(TValueConverter)));

        private void SetConverterInternal(Type sourceType, Type destinationType, TypeEntry<ConverterType> entry)
        {
            converters ??= new ConverterRepository();
            converters.Set(sourceType, destinationType, entry);
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

        internal TypeEntry<FactoryType>? GetFactory() => factory;

        internal IReadOnlyList<TypeEntry<ActionType>> GetBeforeMaps() => beforeMaps ?? (IReadOnlyList<TypeEntry<ActionType>>)Array.Empty<TypeEntry<ActionType>>();

        internal IReadOnlyList<TypeEntry<ActionType>> GetAfterMaps() => afterMaps ?? (IReadOnlyList<TypeEntry<ActionType>>)Array.Empty<TypeEntry<ActionType>>();

        internal Func<string, string?>? GetMatcher() => matcher;

        // Default

        internal TypeEntry<ConverterType>? GetConverter(Type sourceType, Type destinationType)
        {
            return (converters is not null) && converters.TryGetConverter(sourceType, destinationType, out var entry) ? entry : null;
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
