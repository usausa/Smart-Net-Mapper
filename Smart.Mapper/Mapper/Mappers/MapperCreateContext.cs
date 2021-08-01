namespace Smart.Mapper.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Smart.Mapper.Options;

    internal sealed class MemberMapping
    {
        public int No { get; }

        public PropertyInfo Property { get; }

        public TypeEntry<ConditionType>? Condition { get; }

        public FromTypeEntry? MapFrom { get; }

        public bool IsNested { get; }

        public bool IsConst { get; }

        public object? ConstValue { get; }

        public TypeEntry<ConverterType>? Converter { get; }

        public bool IsNullIf { get; }

        public object? NullIfValue { get; }

        public MemberMapping(
            int no,
            PropertyInfo property,
            TypeEntry<ConditionType>? condition,
            FromTypeEntry? mapFrom,
            bool isNested,
            bool isConst,
            object? constValue,
            TypeEntry<ConverterType>? converter,
            bool isNullIf,
            object? nullIfValue)
        {
            No = no;
            Property = property;
            Condition = condition;
            MapFrom = mapFrom;
            IsNested = isNested;
            IsConst = isConst;
            ConstValue = constValue;
            Converter = converter;
            IsNullIf = isNullIf;
            NullIfValue = nullIfValue;
        }
    }

    internal sealed class MapperCreateContext
    {
        private static readonly Func<string, string?> DefaultMatcher = x => x;

        private readonly DefaultOption defaultOption;

        private readonly MappingOption mappingOption;

        public Type SourceType => mappingOption.SourceType;

        public Type DestinationType => mappingOption.DestinationType;

        // Factory

        public bool IsFactoryUseServiceProvider => mappingOption.IsFactoryUseServiceProvider() ||
                                                   defaultOption.IsFactoryUseServiceProvider();

        public TypeEntry<FactoryType>? Factory { get; }

        // Map actions

        public IReadOnlyList<TypeEntry<ActionType>> BeforeMaps => mappingOption.GetBeforeMaps();

        public IReadOnlyList<TypeEntry<ActionType>> AfterMaps => mappingOption.GetAfterMaps();

        // Member

        public IReadOnlyList<MemberMapping> Members { get; }

        // Mapper

        public INestedMapper NexMapper { get; }

        public MapperCreateContext(
            DefaultOption defaultOption,
            MappingOption mappingOption,
            INestedMapper nestedMapper)
        {
            this.defaultOption = defaultOption;
            this.mappingOption = mappingOption;
            NexMapper = nestedMapper;

            Factory = mappingOption.GetFactory();
            if (Factory is null)
            {
                var defaultFactory = defaultOption.GetFactory(DestinationType);
                if (defaultFactory is not null)
                {
                    Factory = new TypeEntry<FactoryType>(FactoryType.FuncDestination, defaultFactory);
                }
            }

            var matcher = mappingOption.GetMatcher() ?? DefaultMatcher;

            var members = new List<MemberMapping>();
            foreach (var memberOption in this.mappingOption.MemberOptions.Where(x => !x.IsIgnore()).OrderBy(x => x.GetOrder()))
            {
                bool isConst;
                object? constValue;
                if (memberOption.UseConst())
                {
                    isConst = true;
                    constValue = memberOption.GetConstValue();
                }
                else
                {
                    isConst = mappingOption.TryGetConstValue(memberOption.Property.PropertyType, out constValue);
                    if (!isConst)
                    {
                        isConst = defaultOption.TryGetConstValue(memberOption.Property.PropertyType, out constValue);
                    }
                }

                if (isConst)
                {
                    members.Add(new MemberMapping(
                        members.Count,
                        memberOption.Property,
                        memberOption.GetCondition(),
                        null,
                        false,
                        true,
                        constValue,
                        (constValue is not null) ? ResolveConverter(constValue.GetType(), memberOption.Property.PropertyType) : null,
                        false,
                        null));
                }
                else
                {
                    var mapFrom = ResolveMapFrom(memberOption, matcher);
                    if (mapFrom is not null)
                    {
                        bool isNullIf;
                        object? nullIfValue;
                        if (memberOption.UseNullIf())
                        {
                            isNullIf = true;
                            nullIfValue = memberOption.GetNullIfValue();
                        }
                        else
                        {
                            isNullIf = mappingOption.TryGetNullIfValue(memberOption.Property.PropertyType, out nullIfValue);
                            if (!isNullIf)
                            {
                                isNullIf = defaultOption.TryGetNullIfValue(memberOption.Property.PropertyType, out nullIfValue);
                            }
                        }

                        members.Add(new MemberMapping(
                            members.Count,
                            memberOption.Property,
                            memberOption.GetCondition(),
                            mapFrom,
                            memberOption.IsNested(),
                            false,
                            null,
                            ResolveConverter(mapFrom.MemberType, memberOption.Property.PropertyType),
                            isNullIf,
                            nullIfValue));
                    }
                }
            }

            Members = members;
        }

        private FromTypeEntry? ResolveMapFrom(MemberOption memberOption, Func<string, string?> matcher)
        {
            var mapFrom = memberOption.GetMapFrom();
            if (mapFrom is not null)
            {
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

            return new FromTypeEntry(FromType.Properties, pi.PropertyType, new[] { pi });
        }

        private TypeEntry<ConverterType>? ResolveConverter(Type sourceType, Type destinationType)
        {
            if (!destinationType.IsAssignableFrom(sourceType))
            {
                return mappingOption.GetConverter(sourceType, destinationType) ??
                       defaultOption.GetConverter(sourceType, destinationType);
            }

            return null;
        }
    }
}
