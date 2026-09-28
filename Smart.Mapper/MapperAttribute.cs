namespace Smart.Mapper;

// Marks a partial method, static or instance, whose implementation the generator writes; the types containing it have
// to be partial, as the generated file declares them again
[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    public bool AutoMap { get; set; } = true;

    // Warns of unmapped destination members, values that may be null and enum members without a match
    public bool Strict { get; set; }

    // Comparison of the member names, in the automatic mapping and in the attributes; an exact match always wins
    public StringComparison NameComparison { get; set; } = StringComparison.Ordinal;

    // Culture name of the conversions (e.g. "ja-JP"); a CultureInfo parameter of the method overrides it
    public string? Culture { get; set; }

    // Format of the date and time conversions, applied to all the date and time types alike
    public string? DateTimeFormat { get; set; }

    // Format of the number conversions
    public string? NumberFormat { get; set; }
}
