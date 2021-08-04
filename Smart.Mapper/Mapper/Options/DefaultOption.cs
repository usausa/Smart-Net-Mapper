namespace Smart.Mapper.Options
{
    using System;
    using System.Collections.Generic;

    using Smart.Mapper.Functions;
    using Smart.Mapper.Helpers;
    using Smart.Mapper.Mappers;

    public sealed class DefaultOption
    {
        private bool factoryUseServiceProvider;

        private Dictionary<Type, object>? factories;

        private readonly ConverterRepository converters = new(DefaultConverters.Entries);

        private Dictionary<Type, object?>? constValues;

        private Dictionary<Type, object?>? nullIfValues;

        private HashSet<Type>? nullIgnores;

        //--------------------------------------------------------------------------------
        // Factory
        //--------------------------------------------------------------------------------

        public void SetFactoryUseServiceProvider() => factoryUseServiceProvider = true;

        public void SetFactory<TDestination>(Func<TDestination> value) => SetFactory(typeof(TDestination), value);

        private void SetFactory(Type type, object value)
        {
            factories ??= new Dictionary<Type, object>();
            factories[type] = value;
        }

        //--------------------------------------------------------------------------------
        // Converter
        //--------------------------------------------------------------------------------

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.FuncSource, value));

        public void SetConverter<TSourceMember, TDestinationMember>(Func<TSourceMember, ResolutionContext, TDestinationMember> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.FuncSourceContext, value));

        public void SetConverter<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> value) =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.Interface, value));

        public void SetConverter<TSourceMember, TDestinationMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TDestinationMember> =>
            SetConverterInternal(typeof(TSourceMember), typeof(TDestinationMember), new TypeEntry<ConverterType>(ConverterType.InterfaceType, typeof(TValueConverter)));

        private void SetConverterInternal(Type sourceType, Type destinationType, TypeEntry<ConverterType> entry)
        {
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

        internal object? GetFactory(Type type)
        {
            if ((factories is not null) && factories.TryGetValue(type, out var value))
            {
                return value;
            }

            return null;
        }

        internal TypeEntry<ConverterType>? GetConverter(Type sourceType, Type destinationType)
        {
            return converters.TryGetConverter(sourceType, destinationType, out var entry) ? entry : null;
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
