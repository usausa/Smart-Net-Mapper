namespace Smart.Mapper;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;

    // Enables strict mode. When true, destination properties that are not mapped (automatically,
    // explicitly, or via MapIgnore) cause a compile-time warning (SMP0501) at the mapper method: those the
    // mapper can assign (an init-only one for a return mapper only, which sets it in the object initializer), and
    // for a return mapper, those only a constructor sets that a constructor it can call takes, when the
    // construction chosen passes them no argument. A member a dotted target path writes into is mapped through the
    // path, and an obsolete property, which the automatic mapping leaves out, is not reported.
    public bool Strict { get; set; }

    // Name comparison used to match member names, both in automatic mapping and for the names of the
    // members written in mapping attributes (targets, properties and fields and each segment of a dotted
    // path, sources, and the member of MapFrom), where an exact match always wins. The names of methods are
    // matched exactly. When not set, the profile's applies, and otherwise Ordinal.
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Culture name (e.g. "ja-JP") used for string conversions. With it, the specialized methods of
    // the value converter are called through their overload taking the culture and the format,
    // (value, IFormatProvider, string?), which the converter has to provide (SMP0104 otherwise).
    public string? Culture { get; set; }

    // Format string applied when converting DateTime / DateOnly / TimeOnly / DateTimeOffset / TimeSpan
    // to or from string. Requires a culture, from Culture or the profile (SMP0401 otherwise). It applies to all of
    // these types alike; a TimeSpan format is written differently (hh\:mm), and one meant for dates fails for a
    // TimeSpan or a TimeOnly at run time, so formats for each type are given with the DateTimeFormat of MapProperty.
    public string? DateTimeFormat { get; set; }

    // Format string applied when converting numeric types to or from string.
    // Requires a culture, from Culture or the profile (SMP0401 otherwise).
    public string? NumberFormat { get; set; }
}
