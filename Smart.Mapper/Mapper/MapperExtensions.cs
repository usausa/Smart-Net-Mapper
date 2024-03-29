namespace Smart.Mapper;

using System.Reflection;

using Smart.Linq;
using Smart.Mapper.Handlers;
using Smart.Mapper.Rules;

public static class MapperExtensions
{
    //--------------------------------------------------------------------------------
    // Config
    //--------------------------------------------------------------------------------

    public static SmartMapper ToMapper(this MapperConfig config) => new(config);

    public static MapperConfig UseServiceProvider<TServiceProvider>(this MapperConfig config)
        where TServiceProvider : IServiceProvider
    {
        config.Configure(static c => c.Add<IServiceProvider, TServiceProvider>());
        return config;
    }

    public static MapperConfig UseServiceProvider(this MapperConfig config, IServiceProvider serviceProvider)
    {
        config.Configure(c => c.Add(serviceProvider));
        return config;
    }

    public static MapperConfig AddMissingHandler<TMissingHandler>(this MapperConfig config)
        where TMissingHandler : IMissingHandler
    {
        config.Configure(static c => c.Add<IMissingHandler, TMissingHandler>());
        return config;
    }

    public static MapperConfig AddMissingHandler(this MapperConfig config, IMissingHandler missingHandler)
    {
        config.Configure(c => c.Add(missingHandler));
        return config;
    }

    public static MapperConfig AddDefaultMapper(this MapperConfig config) =>
        config.AddMissingHandler<DefaultMapperHandler>();

    public static MapperConfig AddRule<TRule>(this MapperConfig config)
        where TRule : IMappingRule
    {
        config.Configure(static c => c.Add<IMappingRule, TRule>());
        return config;
    }

    public static MapperConfig AddRule(this MapperConfig config, IMappingRule rule)
    {
        config.Configure(c => c.Add(rule));
        return config;
    }

    public static MapperConfig AddIgnoreRule(this MapperConfig config, params string[] names) =>
        config.AddRule(new IgnoreRule(x => names.Any(name => x.Name == name)));

    public static MapperConfig AddIgnoreRule(this MapperConfig config, params Type[] types) =>
        config.AddRule(new IgnoreRule(x => types.Any(t => (x is PropertyInfo pi ? pi.PropertyType : ((FieldInfo)x).FieldType) == t)));

    public static MapperConfig AddNestedRule(this MapperConfig config, Func<MemberInfo, bool> predicate, string? profile = null) =>
        config.AddRule(new NestedRule(predicate, profile));
}
