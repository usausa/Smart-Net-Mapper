namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

// Represents a custom parameter passed to a mapper method.
internal sealed record CustomParameterModel(
    string Name,
    // Without nullable annotations, which is what types are compared by
    string TypeName,
    // As declared, nullable annotations included, for the implementation and the local functions of
    // [MapExpression] (CS8611 otherwise)
    string DeclaredTypeName = "",
    // Declared modifiers (in, ref readonly, ref, scoped, params), which the implementation repeats, and
    // the RefKind, which decides how the parameter is passed on to the local function of a [MapExpression]
    string Modifiers = "",
    RefKind RefKind = default);
