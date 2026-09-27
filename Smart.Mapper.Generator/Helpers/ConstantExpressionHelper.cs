namespace Smart.Mapper.Generator.Helpers;

using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

// Writes an attribute argument as the C# expression the generated code assigns: a literal of the constant's
// type, spelled the same whatever culture the generator runs under (a byte, sbyte, short or ushort, which has
// no literal of its own, as a cast), a member of an enum (a cast of its number for a value no member has, such
// as a combination of flags, or only members marked [Obsolete] have), typeof for a type, and an array creation
// for an array.
internal static class ConstantExpressionHelper
{
    // The expression, or null when the constant cannot be written into the generated code: an argument with an
    // error, or a type the generated file cannot name (a file-local type, visible only in the file declaring
    // it, or a pointer, which needs an unsafe context).
    public static string? Format(TypedConstant constant) => Format(constant, typed: true);

    // Typed keeps the type of a number written as a cast. The elements of an array of that type go without
    // the cast, as the array creation converts them.
    private static string? Format(TypedConstant constant, bool typed)
    {
        if (constant.Kind == TypedConstantKind.Error)
        {
            return null;
        }

        if (constant.IsNull)
        {
            return "null";
        }

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => FormatPrimitive(constant.Value, typed),
            TypedConstantKind.Enum => FormatEnum(constant.Type, constant.Value),
            TypedConstantKind.Type => (constant.Value is ITypeSymbol type) && CanReferTo(type)
                ? "typeof(" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")"
                : null,
            TypedConstantKind.Array => FormatArray(constant),
            _ => null
        };
    }

    // Whether the constant is an array holding null. Its element type is then written nullable, which keeps
    // the array creation free of nullable warnings, and the target has to take null elements.
    public static bool HasNullElement(TypedConstant constant) =>
        (constant.Kind == TypedConstantKind.Array) && !constant.IsNull && constant.Values.Any(static v => v.IsNull);

    // A string value the generated code passes on, such as a culture name or a format, as a literal with the
    // quotes, backslashes and control characters in it escaped.
    public static string FormatString(string value) => SymbolDisplay.FormatLiteral(value, quote: true);

    // A byte, sbyte, short or ushort is written as a cast, so that it keeps its type where the target takes any
    // (an object is boxed as it, not as an int). A cast of a negative number needs no parentheses, as the type
    // is a keyword.
    private static string? FormatPrimitive(object? value, bool typed) => value switch
    {
        string s => SymbolDisplay.FormatLiteral(s, quote: true),
        char c => SymbolDisplay.FormatLiteral(c, quote: true),
        bool b => b ? "true" : "false",
        float f => FormatSingle(f),
        double d => FormatDouble(d),
        decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
        long l => l.ToString(CultureInfo.InvariantCulture) + "L",
        ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "UL",
        uint ui => ui.ToString(CultureInfo.InvariantCulture) + "U",
        int i => i.ToString(CultureInfo.InvariantCulture),
        short or ushort or byte or sbyte => (typed ? "(" + GetKeyword(value) + ")" : string.Empty) + ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
        _ => null
    };

    private static string GetKeyword(object value) => value switch
    {
        short => "short",
        ushort => "ushort",
        byte => "byte",
        _ => "sbyte"
    };

    // R gives the shortest text that parses back to the same value, which the .NET Framework does not always
    // manage, so G9 / G17 is taken when it did not. The sign of a negative zero, which the .NET Framework drops,
    // is put back.
    private static string FormatSingle(float value)
    {
        if (Single.IsNaN(value))
        {
            return "float.NaN";
        }

        if (Single.IsInfinity(value))
        {
            return value > 0 ? "float.PositiveInfinity" : "float.NegativeInfinity";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (!Single.Parse(text, CultureInfo.InvariantCulture).Equals(value))
        {
            text = value.ToString("G9", CultureInfo.InvariantCulture);
        }

        return WithNegativeZeroSign(text, value) + "f";
    }

    private static string FormatDouble(double value)
    {
        if (Double.IsNaN(value))
        {
            return "double.NaN";
        }

        if (Double.IsInfinity(value))
        {
            return value > 0 ? "double.PositiveInfinity" : "double.NegativeInfinity";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (!Double.Parse(text, CultureInfo.InvariantCulture).Equals(value))
        {
            text = value.ToString("G17", CultureInfo.InvariantCulture);
        }

        return WithNegativeZeroSign(text, value) + "d";
    }

    private static string WithNegativeZeroSign(string text, double value) =>
        (BitConverter.DoubleToInt64Bits(value) == Int64.MinValue) && (text[0] != '-') ? "-" + text : text;

    // A value is written as the first member declared with it, and as a cast of its number when no member has
    // it. A member marked [Obsolete] is passed over, as naming it warns (CS0618) or fails (CS0619), so a value
    // only such members have is written as a cast too.
    private static string? FormatEnum(ITypeSymbol? type, object? value)
    {
        if ((type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType) || !CanReferTo(enumType))
        {
            return null;
        }

        var typeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        foreach (var member in enumType.GetMembers())
        {
            if ((member is IFieldSymbol { HasConstantValue: true } field) && Object.Equals(field.ConstantValue, value) &&
                (field.GetObsoleteKind() == ObsoleteKind.None))
            {
                return typeName + "." + IdentifierHelper.Escape(field.Name);
            }
        }

        var number = FormatEnumNumber(value);
        return number is null ? null : FormatEnumCast(typeName, number);
    }

    // The number of an enum value, to be cast to the enum type: a literal of the underlying type, without a cast
    // of its own. Null for a value that is not a number.
    public static string? FormatEnumNumber(object? value) => FormatPrimitive(value, typed: false);

    // A cast of the number to the enum type. A negative number is parenthesized, as (T)-1 would parse as a
    // subtraction.
    public static string FormatEnumCast(string typeName, string number) =>
        number[0] == '-' ? "(" + typeName + ")(" + number + ")" : "(" + typeName + ")" + number;

    private static string? FormatArray(TypedConstant constant)
    {
        if ((constant.Type is not IArrayTypeSymbol arrayType) || !CanReferTo(arrayType.ElementType))
        {
            return null;
        }

        var elements = new List<string>(constant.Values.Length);
        foreach (var value in constant.Values)
        {
            var element = Format(value, typed: !SymbolEqualityComparer.Default.Equals(value.Type, arrayType.ElementType));
            if (element is null)
            {
                return null;
            }

            elements.Add(element);
        }

        var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (arrayType.ElementType.IsReferenceType && HasNullElement(constant))
        {
            elementType += "?";
        }

        return elements.Count == 0
            ? "new " + elementType + "[0]"
            : "new " + elementType + "[] { " + String.Join(", ", elements) + " }";
    }

    // Whether the generated file can name the type: not one with an error, not a file-local type or one nested
    // in it, and not a pointer. The type arguments of an unbound generic type, as in typeof(List<>), are left
    // out.
    private static bool CanReferTo(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return CanReferTo(array.ElementType);
            case INamedTypeSymbol named:
                for (var current = named; current is not null; current = current.ContainingType)
                {
                    if ((current.TypeKind == TypeKind.Error) || current.IsFileLocal ||
                        (!current.IsUnboundGenericType && !current.TypeArguments.All(CanReferTo)))
                    {
                        return false;
                    }
                }

                return true;
            default:
                return type.TypeKind is not (TypeKind.Error or TypeKind.Pointer or TypeKind.FunctionPointer);
        }
    }
}
