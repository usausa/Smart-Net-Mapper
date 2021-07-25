namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Smart.Mapper.Options;

    public sealed class MemberMapping
    {
        public int No { get; }

        public PropertyInfo Property { get; }

        public bool IsNested { get; }

        public object? Condition { get; }

        public object? MapFrom { get; }

        public object? Converter { get; }

        public bool IsConst { get; }

        public object? ConstValue { get; }

        public bool IsNullIf { get; }

        public object? NullIfValue { get; }

        public bool IsNullIgnore { get; }

        public MemberMapping(
            int no,
            PropertyInfo property,
            bool isNested,
            object? condition,
            object? mapFrom,
            object? converter,
            bool isConst,
            object? constValue,
            bool isNullIf,
            object? nullIfValue,
            bool isNullIgnore)
        {
            No = no;
            Property = property;
            IsNested = isNested;
            Condition = condition;
            MapFrom = mapFrom;
            Converter = converter;
            IsConst = isConst;
            ConstValue = constValue;
            IsNullIf = isNullIf;
            NullIfValue = nullIfValue;
            IsNullIgnore = isNullIgnore;
        }
    }

    public sealed class MapperCreateContext
    {
        private static readonly Func<string, string?> DefaultMatcher = x => x;

        private readonly DefaultOption defaultOption;

        private readonly MappingOption mappingOption;

        public Type SourceType => mappingOption.SourceType;

        public Type DestinationType => mappingOption.DestinationType;

        // Factory

        public bool IsFactoryUseServiceProvider => mappingOption.IsFactoryUseServiceProvider() ||
                                                   defaultOption.IsFactoryUseServiceProvider();

        public object? Factory => mappingOption.GetFactory() ?? defaultOption.GetFactory(DestinationType);

        // Map actions

        public IReadOnlyList<object> BeforeMaps => mappingOption.GetBeforeMaps();

        public IReadOnlyList<object> AfterMaps => mappingOption.GetAfterMaps();

        // Member

        public IReadOnlyList<MemberMapping> Members { get; }

        public MapperCreateContext(
            DefaultOption defaultOption,
            MappingOption mappingOption)
        {
            this.defaultOption = defaultOption;
            this.mappingOption = mappingOption;

            var matcher = mappingOption.GetMatcher() ?? DefaultMatcher;

            var members = new List<MemberMapping>();
            foreach (var memberOption in this.mappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                var mapFrom = ResolveMapFrom(memberOption, matcher);
                if (mapFrom is null)
                {
                    continue;
                }

                bool isConst;
                object? constValue;
                if (memberOption.UseConst())
                {
                    isConst = true;
                    constValue = memberOption.GetConstValue();
                }
                else
                {
                    isConst = defaultOption.TryGetConstValue(memberOption.Property.PropertyType, out constValue);
                }

                bool isNullIf;
                object? nullIfValue;
                if (memberOption.UseNullIf())
                {
                    isNullIf = true;
                    nullIfValue = memberOption.GetNullIfValue();
                }
                else
                {
                    isNullIf = defaultOption.TryGetNullIfValue(memberOption.Property.PropertyType, out nullIfValue);
                }

                members.Add(new MemberMapping(
                    members.Count,
                    memberOption.Property,
                    memberOption.IsNested(),
                    memberOption.GetCondition(),
                    mapFrom,
                    memberOption.GetConverter(),
                    isConst,
                    constValue,
                    isNullIf,
                    nullIfValue,
                    memberOption.IsNullIgnore() || defaultOption.IsNullIgnore(memberOption.Property.PropertyType)));
            }

            Members = members;
        }

        private object? ResolveMapFrom(MemberOption memberOption, Func<string, string?> matcher)
        {
            var mapFrom = memberOption.GetMapFrom();
            if (mapFrom is not null)
            {
                var type = mapFrom.GetType();
                if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(Lazy<>)))
                {
                    mapFrom = mapFrom.GetType().GetProperty("Value")!.GetValue(mapFrom);
                }

                return mapFrom;
            }

            // Default
            var name = matcher(memberOption.Property.Name) ?? memberOption.Property.Name;
            if (String.IsNullOrEmpty(name))
            {
                return null;
            }

            var pi = mappingOption.SourceType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if ((pi is null) || !pi.CanRead)
            {
                return null;
            }

            return pi;
        }

        public bool TryGetConverter(Tuple<Type, Type> pair, [NotNullWhen(true)] out object? value) =>
            defaultOption.TryGetConverter(pair, out value);
    }
}
