namespace Smart.Mapper.Generator.Helpers;

using System;

/// <summary>
/// Utilities for working with fully-qualified C# type name strings as produced by
/// <c>ITypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)</c>.
/// </summary>
internal static class TypeNameHelper
{
    // -------------------------------------------------------
    // Type category checks
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when the type name represents <see cref="System.String"/>,
    /// handling both the alias form ("string") and the fully-qualified form.
    /// </summary>
    public static bool IsStringType(string typeName) =>
        typeName is "string" or "global::System.String" or "global::System.String?";

    /// <summary>
    /// Returns true when the type name is a DateTime-family type:
    /// DateTime, DateOnly, TimeOnly, DateTimeOffset, or TimeSpan.
    /// </summary>
    public static bool IsDateTimeType(string typeName) =>
        typeName is "global::System.DateTime" or "global::System.DateOnly" or
                    "global::System.TimeOnly" or "global::System.DateTimeOffset" or
                    "global::System.TimeSpan";

    /// <summary>
    /// Returns true when the fully-qualified type name is one of the built-in numeric or date/time types
    /// that <c>DefaultValueConverter</c> has specialised overloads for.
    /// </summary>
    public static bool IsBuiltInNumericOrDateType(string typeName) =>
        typeName is
            "global::System.SByte"   or "global::System.Byte"
         or "global::System.Int16"   or "global::System.UInt16"
         or "global::System.Int32"   or "global::System.UInt32"
         or "global::System.Int64"   or "global::System.UInt64"
         or "global::System.Single"  or "global::System.Double"
         or "global::System.Decimal" or "global::System.Boolean"
         or "global::System.DateTime" or "global::System.Guid"
         or "global::System.DateOnly" or "global::System.TimeOnly"
         or "global::System.DateTimeOffset" or "global::System.TimeSpan"
         or "global::System.Half"    or "global::System.Int128"
         or "global::System.UInt128" or "global::System.Numerics.BigInteger"
         or "sbyte" or "byte" or "short" or "ushort" or "int" or "uint"
         or "long" or "ulong" or "float" or "double" or "decimal" or "bool";

    // -------------------------------------------------------
    // Type name normalisation
    // -------------------------------------------------------

    /// <summary>
    /// Strips <c>global::</c> and <c>System.</c> prefixes and removes trailing <c>?</c>
    /// or <c>Nullable&lt;&gt;</c> wrappers so that two equivalent type names compare equal.
    /// </summary>
    public static string NormalizeTypeName(string typeName)
    {
        var normalized = typeName
            .Replace("global::", string.Empty)
            .Replace("System.", string.Empty);

        if (normalized.EndsWith("?", StringComparison.Ordinal))
        {
            normalized = normalized.TrimEnd('?');
        }

        if (normalized.StartsWith("Nullable<", StringComparison.Ordinal) &&
            normalized.EndsWith(">", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(9, normalized.Length - 10);
        }

        return normalized;
    }

    /// <summary>
    /// Returns the simple (unqualified) type name, mapping C# aliases to their CLR names.
    /// For example: <c>"global::System.Int32"</c> → <c>"Int32"</c>, <c>"int"</c> → <c>"Int32"</c>.
    /// </summary>
    public static string GetSimpleTypeName(string fullyQualifiedTypeName) =>
        fullyQualifiedTypeName switch
        {
            "int"                  or "global::System.Int32"    => "Int32",
            "long"                 or "global::System.Int64"    => "Int64",
            "short"                or "global::System.Int16"    => "Int16",
            "byte"                 or "global::System.Byte"     => "Byte",
            "sbyte"                or "global::System.SByte"    => "SByte",
            "uint"                 or "global::System.UInt32"   => "UInt32",
            "ulong"                or "global::System.UInt64"   => "UInt64",
            "ushort"               or "global::System.UInt16"   => "UInt16",
            "float"                or "global::System.Single"   => "Single",
            "double"               or "global::System.Double"   => "Double",
            "decimal"              or "global::System.Decimal"  => "Decimal",
            "bool"                 or "global::System.Boolean"  => "Boolean",
            "string"               or "global::System.String"   => "String",
            "global::System.DateTime"                           => "DateTime",
            "global::System.Guid"                               => "Guid",
            _                                                   => ExtractLastSegment(fullyQualifiedTypeName)
        };

    /// <summary>
    /// Returns the last dot-separated segment of a fully-qualified type name,
    /// stripping any leading <c>global::</c> prefix first.
    /// </summary>
    public static string ExtractLastSegment(string fullyQualifiedTypeName)
    {
        var name = fullyQualifiedTypeName;
        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name.Substring("global::".Length);
        }

        var lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name.Substring(lastDot + 1) : name;
    }

    // -------------------------------------------------------
    // Numeric conversion classification
    // -------------------------------------------------------

    /// <summary>
    /// Returns true when the conversion from <paramref name="sourceType"/> to
    /// <paramref name="targetType"/> requires an explicit cast rather than being a
    /// widening implicit numeric conversion.
    /// </summary>
    public static bool RequiresTypeConversion(string sourceType, string targetType)
    {
        var normalizedSource = NormalizeTypeName(sourceType);
        var normalizedTarget = NormalizeTypeName(targetType);

        if (normalizedSource == normalizedTarget)
        {
            return false;
        }

        if (IsImplicitNumericConversion(normalizedSource, normalizedTarget))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true when C# allows an implicit widening numeric conversion from
    /// <paramref name="sourceType"/> to <paramref name="targetType"/>.
    /// Type names should be normalized before calling.
    /// </summary>
    public static bool IsImplicitNumericConversion(string sourceType, string targetType) =>
        sourceType switch
        {
            "sbyte" or "SByte" => targetType is "short" or "Int16" or "int" or "Int32" or "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "byte" or "Byte" => targetType is "short" or "Int16" or "ushort" or "UInt16"
                or "int" or "Int32" or "uint" or "UInt32" or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "short" or "Int16" => targetType is "int" or "Int32" or "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "ushort" or "UInt16" => targetType is "int" or "Int32" or "uint" or "UInt32"
                or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "int" or "Int32" => targetType is "long" or "Int64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "uint" or "UInt32" => targetType is "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "long" or "Int64" => targetType is "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "ulong" or "UInt64" => targetType is "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            "float" or "Single" => targetType is "double" or "Double",
            "char" or "Char" => targetType is "ushort" or "UInt16" or "int" or "Int32" or "uint" or "UInt32"
                or "long" or "Int64" or "ulong" or "UInt64"
                or "float" or "Single" or "double" or "Double" or "decimal" or "Decimal",
            _ => false
        };

    /// <summary>
    /// Returns true when both types are numeric and the conversion is a narrowing (explicit) cast
    /// that is not already covered by implicit widening rules.
    /// </summary>
    public static bool IsExplicitNumericConversion(string sourceType, string targetType)
    {
        static bool IsNumeric(string t) =>
            t is "sbyte" or "SByte" or "global::System.SByte"
              or "byte" or "Byte" or "global::System.Byte"
              or "short" or "Int16" or "global::System.Int16"
              or "ushort" or "UInt16" or "global::System.UInt16"
              or "int" or "Int32" or "global::System.Int32"
              or "uint" or "UInt32" or "global::System.UInt32"
              or "long" or "Int64" or "global::System.Int64"
              or "ulong" or "UInt64" or "global::System.UInt64"
              or "float" or "Single" or "global::System.Single"
              or "double" or "Double" or "global::System.Double"
              or "decimal" or "Decimal" or "global::System.Decimal"
              or "char" or "Char" or "global::System.Char"
              or "Half" or "global::System.Half"
              or "Int128" or "global::System.Int128"
              or "UInt128" or "global::System.UInt128";

        if (!IsNumeric(sourceType) || !IsNumeric(targetType))
        {
            return false;
        }

        var normalizedSource = NormalizeTypeName(sourceType);
        var normalizedTarget = NormalizeTypeName(targetType);

        if (normalizedSource == normalizedTarget)
        {
            return false;
        }

        return !IsImplicitNumericConversion(normalizedSource, normalizedTarget);
    }
}
