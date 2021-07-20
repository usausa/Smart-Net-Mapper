namespace Smart.Mapper.Options
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Smart.Mapper.Functions;

    public sealed class DefaultOption
    {
        private bool factoryUseServiceProvider;

        private Dictionary<Type, object>? factories;

        private Dictionary<Tuple<Type, Type>, object>? converters;

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

        internal object? GetFactory(Type type)
        {
            if ((factories is not null) && factories.TryGetValue(type, out var value))
            {
                return value;
            }

            return null;
        }

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
