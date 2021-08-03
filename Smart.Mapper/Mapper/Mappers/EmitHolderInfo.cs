namespace Smart.Mapper.Mappers
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    internal sealed class EmitHolderInfo
    {
        public object Instance { get; }

        public bool HasDestinationParameter { get; }

        public bool HasContext { get; }

        public EmitHolderInfo(
            MapperCreateContext context,
            TypeBuilder typeBuilder,
            IServiceProvider serviceProvider)
        {
            HasDestinationParameter = IsDestinationParameterRequired(context);
            HasContext = IsContextRequired(context);

            // Nested mapper
            if (HasContext)
            {
                typeBuilder.DefineField("mapper", typeof(INestedMapper), FieldAttributes.Public);
            }

            // TODO Nested

            // Factory
            var factory = ResolveFactory(serviceProvider, context.Factory);
            if (context.IsFactoryUseServiceProvider)
            {
                typeBuilder.DefineField("factory", typeof(IServiceProvider), FieldAttributes.Public);
            }
            else if (factory is not null)
            {
                typeBuilder.DefineField("factory", factory.GetType(), FieldAttributes.Public);
            }

            // BeforeMap
            var beforeMaps = context.BeforeMaps.Select(x => ResolveAction(serviceProvider, x)).ToList();
            for (var i = 0; i < beforeMaps.Count; i++)
            {
                typeBuilder.DefineField($"beforeMap{i}", beforeMaps[i].GetType(), FieldAttributes.Public);
            }

            // AfterMap
            var afterMaps = context.AfterMaps.Select(x => ResolveAction(serviceProvider, x)).ToList();
            for (var i = 0; i < afterMaps.Count; i++)
            {
                typeBuilder.DefineField($"afterMap{i}", afterMaps[i].GetType(), FieldAttributes.Public);
            }

            // Condition
            var conditions = context.Members.Where(x => x.Condition is not null)
                .Select(x => new { Member = x, Condition = ResolveCondition(serviceProvider, x.Condition!) })
                .ToList();
            foreach (var condition in conditions)
            {
                typeBuilder.DefineField($"condition{condition.Member.No}", condition.Condition.GetType(), FieldAttributes.Public);
            }

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                typeBuilder.DefineField($"constValue{member.No}", member.Property.PropertyType, FieldAttributes.Public);
            }

            // Provider
            var providers = context.Members.Where(x => x.MapFrom is not null)
                .Select(x => new { Member = x, Provider = ResolveProvider(serviceProvider, x.MapFrom!) })
                .Where(x => x.Provider is not null)
                .ToList();
            foreach (var provider in providers)
            {
                typeBuilder.DefineField($"provider{provider.Member.No}", provider.Provider!.GetType(), FieldAttributes.Public);
            }

            // Converter
            var converters = context.Members.Where(x => x.Converter is not null)
                .Select(x => new { Member = x, Converter = ResolveConverter(serviceProvider, x.Converter!) })
                .ToList();
            foreach (var converter in converters)
            {
                typeBuilder.DefineField($"converter{converter.Member.No}", converter.Converter.GetType(), FieldAttributes.Public);
            }

            // NullIf
            foreach (var member in context.Members.Where(x => x.IsNullIf))
            {
                typeBuilder.DefineField($"nullIfValue{member.No}", member.Property.PropertyType, FieldAttributes.Public);
            }

            // Create holder
            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            Instance = Activator.CreateInstance(holderType)!;

            // Nested mapper
            if (HasContext)
            {
                GetMapperField().SetValue(Instance, context.NexMapper);
            }

            // TODO Nested

            // Factory
            if (context.IsFactoryUseServiceProvider)
            {
                GetFactoryField().SetValue(Instance, serviceProvider);
            }
            else if (factory is not null)
            {
                GetFactoryField().SetValue(Instance, factory);
            }

            // BeforeMap
            for (var i = 0; i < beforeMaps.Count; i++)
            {
                GetBeforeMapField(i).SetValue(Instance, beforeMaps[i]);
            }

            // AfterMap
            for (var i = 0; i < afterMaps.Count; i++)
            {
                GetAfterMapField(i).SetValue(Instance, afterMaps[i]);
            }

            // Condition
            foreach (var condition in conditions)
            {
                GetConditionField(condition.Member.No).SetValue(Instance, condition.Condition);
            }

            // Const
            foreach (var member in context.Members.Where(x => x.IsConst))
            {
                GetConstValueField(member.No).SetValue(Instance, member.ConstValue);
            }

            // Provider
            foreach (var provider in providers)
            {
                GetProviderField(provider.Member.No).SetValue(Instance, provider.Provider);
            }

            // Converter
            foreach (var converter in converters)
            {
                GetConverterField(converter.Member.No).SetValue(Instance, converter.Converter);
            }

            // NullIf
            foreach (var member in context.Members.Where(x => x.IsNullIf))
            {
                GetNullIfValueField(member.No).SetValue(Instance, member.NullIfValue);
            }
        }

        private static bool IsDestinationParameterRequired(MapperCreateContext context)
        {
            if (context.BeforeMaps.Any(x => x.Type.HasDestinationParameter()) ||
                context.AfterMaps.Any(x => x.Type.HasDestinationParameter()))
            {
                return true;
            }

            return context.Members.Any(x => ((x.MapFrom is not null) && x.MapFrom.Type.HasDestinationParameter()) ||
                                            ((x.Condition is not null) && x.Condition.Type.HasDestinationParameter()));
        }

        private static bool IsContextRequired(MapperCreateContext context)
        {
            if ((context.Factory is not null) && context.Factory.Type.HasContext())
            {
                return true;
            }

            if (context.BeforeMaps.Any(x => x.Type.HasContext()) ||
                context.AfterMaps.Any(x => x.Type.HasContext()))
            {
                return true;
            }

            return context.Members.Any(x => ((x.MapFrom is not null) && x.MapFrom.Type.HasContext()) ||
                                            ((x.Condition is not null) && x.Condition.Type.HasContext()) ||
                                            ((x.Converter is not null) && x.Converter.Type.HasContext()));
        }

        //--------------------------------------------------------------------------------
        // Resolve
        //--------------------------------------------------------------------------------

        private static object? ResolveFactory(IServiceProvider serviceProvider, TypeEntry<FactoryType>? entry)
        {
            if (entry is null)
            {
                return null;
            }

            return entry.Type == FactoryType.InterfaceType ? serviceProvider.GetService((Type)entry.Value) : entry.Value;
        }

        private static object ResolveAction(IServiceProvider serviceProvider, TypeEntry<ActionType> entry) =>
            entry.Type == ActionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        private static object ResolveCondition(IServiceProvider serviceProvider, TypeEntry<ConditionType> entry) =>
            entry.Type == ConditionType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        private static object? ResolveProvider(IServiceProvider serviceProvider, FromTypeEntry entry)
        {
            switch (entry.Type)
            {
                case FromType.LazyFunc:
                    return entry.Value.GetType().GetProperty("Value")!.GetValue(entry.Value);
                case FromType.Func:
                case FromType.FuncContext:
                case FromType.Interface:
                    return entry.Value;
                case FromType.InterfaceType:
                    return serviceProvider.GetService((Type)entry.Value)!;
            }

            return null;
        }

        private static object ResolveConverter(IServiceProvider serviceProvider, TypeEntry<ConverterType> entry) =>
             entry.Type == ConverterType.InterfaceType ? serviceProvider.GetService((Type)entry.Value)! : entry.Value;

        //--------------------------------------------------------------------------------
        // Field
        //--------------------------------------------------------------------------------

        public FieldInfo GetMapperField() => Instance.GetType().GetField("mapper")!;

        public FieldInfo GetFactoryField() => Instance.GetType().GetField("factory")!;

        public FieldInfo GetBeforeMapField(int index) => Instance.GetType().GetField($"beforeMap{index}")!;

        public FieldInfo GetAfterMapField(int index) => Instance.GetType().GetField($"afterMap{index}")!;

        public FieldInfo GetConditionField(int index) => Instance.GetType().GetField($"condition{index}")!;

        public FieldInfo GetConstValueField(int index) => Instance.GetType().GetField($"constValue{index}")!;

        public FieldInfo GetProviderField(int index) => Instance.GetType().GetField($"provider{index}")!;

        public FieldInfo GetConverterField(int index) => Instance.GetType().GetField($"converter{index}")!;

        public FieldInfo GetNullIfValueField(int index) => Instance.GetType().GetField($"nullIfValue{index}")!;
    }
}