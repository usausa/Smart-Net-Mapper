namespace Smart.Mapper.Generator.Models;

using System.Linq;

internal static class PropertyMappingModelExtensions
{
    public static bool IsEnumMapping(this PropertyMappingModel m) => m.EnumMappingKind != EnumMappingKind.None;

    public static bool HasConverter(this PropertyMappingModel m) => !string.IsNullOrEmpty(m.ConverterMethod);

    public static bool HasSpecializedConverter(this PropertyMappingModel m) => !string.IsNullOrEmpty(m.SpecializedConverterMethod);

    public static bool HasParsableMethod(this PropertyMappingModel m) => m.ParseMethod != ParseMethodKind.None;

    public static bool HasUserDefinedExplicit(this PropertyMappingModel m) => m.UserDefinedConversion == UserDefinedConversionKind.Explicit;

    public static bool HasCondition(this PropertyMappingModel m) => !string.IsNullOrEmpty(m.ConditionMethod);

    public static bool HasNullSubstitute(this PropertyMappingModel m) => !string.IsNullOrEmpty(m.NullSubstitute);

    public static bool HasCulture(this PropertyMappingModel m) => !string.IsNullOrEmpty(m.EffectiveCulture);

    public static bool IsSourceNested(this PropertyMappingModel m) => m.SourcePath.Contains('.');

    public static bool IsTargetNested(this PropertyMappingModel m) => m.TargetPath.Contains('.');

    public static bool RequiresNullCheck(this PropertyMappingModel m) =>
        m.SourcePathSegments.ToArray().Any(s => s.IsNullable);

    public static bool RequiresNullCoalescing(this PropertyMappingModel m) =>
        m.IsSourceNullable && !m.IsTargetNullable && m.NullBehavior == NullBehaviorType.Default && !m.HasNullSubstitute();
}
