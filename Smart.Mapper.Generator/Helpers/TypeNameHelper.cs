namespace Smart.Mapper.Generator.Helpers;

using System;

// Utilities for working with fully-qualified C# type name strings as produced by
// ITypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).
internal static class TypeNameHelper
{
    // -------------------------------------------------------
    // Type category checks
    // -------------------------------------------------------

    // Returns true when the type name represents System.String,
    // handling both the alias form ("string") and the fully-qualified form.
    public static bool IsStringType(string typeName) =>
        typeName is "string" or "global::System.String" or "global::System.String?";

    // Returns true when the type name is a DateTime-family type:
    // DateTime, DateOnly, TimeOnly, DateTimeOffset, or TimeSpan.
    public static bool IsDateTimeType(string typeName) =>
        typeName is "global::System.DateTime" or "global::System.DateOnly" or
                    "global::System.TimeOnly" or "global::System.DateTimeOffset" or
                    "global::System.TimeSpan";

    // Returns true when the fully-qualified type name is one of the built-in numeric or date/time types
    // that DefaultValueConverter has specialised overloads for.
    public static bool IsBuiltInNumericOrDateType(string typeName) =>
        typeName is
            "global::System.SByte" or "global::System.Byte"
         or "global::System.Int16" or "global::System.UInt16"
         or "global::System.Int32" or "global::System.UInt32"
         or "global::System.Int64" or "global::System.UInt64"
         or "global::System.Single" or "global::System.Double"
         or "global::System.Decimal" or "global::System.Boolean"
         or "global::System.DateTime" or "global::System.Guid"
         or "global::System.DateOnly" or "global::System.TimeOnly"
         or "global::System.DateTimeOffset" or "global::System.TimeSpan"
         or "global::System.Half" or "global::System.Int128"
         or "global::System.UInt128" or "global::System.Numerics.BigInteger"
         or "sbyte" or "byte" or "short" or "ushort" or "int" or "uint"
         or "long" or "ulong" or "float" or "double" or "decimal" or "bool";

    // -------------------------------------------------------
    // Type name normalisation
    // -------------------------------------------------------

    // Strips the global:: qualifier and a leading System. namespace, and removes trailing ?
    // or Nullable<> wrappers so that two equivalent type names compare equal.
    // System. is stripped only as a leading namespace qualifier (not as an arbitrary substring),
    // so user namespaces that merely contain "System." as a segment are preserved.
    public static string NormalizeTypeName(string typeName)
    {
        var normalized = typeName.Replace("global::", string.Empty);

        // Unwrap nullable value types: a trailing "?" or a Nullable<...> wrapper.
        if (normalized.EndsWith("?", StringComparison.Ordinal))
        {
            normalized = normalized.TrimEnd('?');
        }
        else if (normalized.EndsWith(">", StringComparison.Ordinal))
        {
            var open = normalized.IndexOf("Nullable<", StringComparison.Ordinal);
            if (open >= 0)
            {
                var start = open + "Nullable<".Length;
                normalized = normalized.Substring(start, normalized.Length - start - 1);
            }
        }

        if (normalized.StartsWith("System.", StringComparison.Ordinal))
        {
            normalized = normalized.Substring("System.".Length);
        }

        return normalized;
    }

    // Returns the simple (unqualified) type name, mapping C# aliases to their CLR names.
    // For example: "global::System.Int32" → "Int32", "int" → "Int32".
    public static string GetSimpleTypeName(string fullyQualifiedTypeName) =>
        fullyQualifiedTypeName switch
        {
            "int" or "global::System.Int32" => "Int32",
            "long" or "global::System.Int64" => "Int64",
            "short" or "global::System.Int16" => "Int16",
            "byte" or "global::System.Byte" => "Byte",
            "sbyte" or "global::System.SByte" => "SByte",
            "uint" or "global::System.UInt32" => "UInt32",
            "ulong" or "global::System.UInt64" => "UInt64",
            "ushort" or "global::System.UInt16" => "UInt16",
            "float" or "global::System.Single" => "Single",
            "double" or "global::System.Double" => "Double",
            "decimal" or "global::System.Decimal" => "Decimal",
            "bool" or "global::System.Boolean" => "Boolean",
            "string" or "global::System.String" => "String",
            "global::System.DateTime" => "DateTime",
            "global::System.Guid" => "Guid",
            _ => ExtractLastSegment(fullyQualifiedTypeName)
        };

    // Returns the last dot-separated segment of a fully-qualified type name,
    // stripping any leading global:: prefix first.
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

    // Returns true when the conversion from sourceType to targetType requires an explicit cast
    // rather than being a widening implicit numeric conversion.
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

    // Returns true when C# allows an implicit widening numeric conversion from
    // sourceType to targetType. Type names should be normalized before calling.
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

    // Returns true when both types are numeric and the conversion is a narrowing (explicit) cast
    // that is not already covered by implicit widening rules.
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
