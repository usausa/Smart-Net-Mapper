namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

// Represents a custom parameter passed to a mapper method.
internal sealed record CustomParameterModel(
    // As the generated code writes it, a keyword with its @
    string Name,
    // As declared, nullable annotations included, for the implementation and the local functions of
    // [MapExpression] (CS8611 otherwise); the methods taking it are matched by the symbol of its type
    string DeclaredTypeName,
    // Declared modifiers (in, ref readonly, ref, scoped, params), which the implementation repeats, and
    // the RefKind, which decides how the parameter is passed on to the local function of a [MapExpression]
    string Modifiers = "",
    RefKind RefKind = default,
    // For the CultureInfo parameter that may be null, the argument a parameter not taking null gets instead of it: the
    // culture the conversions go with, which falls back for null as theirs does ("(culture ?? __culture_de_DE)")
    string? NonNullArgument = default);
