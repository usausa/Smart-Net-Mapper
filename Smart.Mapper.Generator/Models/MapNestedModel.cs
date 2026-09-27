namespace Smart.Mapper.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// Represents a MapNested mapping (nested object property mapped using a mapper method).
internal sealed record MapNestedModel(
    // Source and target members
    string SourceName = default!,
    string SourceType = default!,
    string TargetName = default!,
    string TargetType = default!,
    // The type the local of a constructor argument is declared as: the target type, with its nullable annotations
    // when the mapper returns a nullable reference into a target annotated as one, which the local has to take
    string ArgumentLocalType = default!,
    // Mapper method that maps the nested object. The RefKinds of its parameters decide how the instance a
    // void mapper fills is passed
    string Mapper = default!,
    EquatableArray<RefKind> MapperParameterRefKinds = default,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Emit shape. A value-returning mapper is assigned, a void one fills the existing instance
    bool MapperReturnsValue = default,
    // The mapper returns a nullable reference into a target not annotated as one, so its result is taken with !
    bool ForgivesMapperResult = default,
    // The source is a nullable struct the mapper takes the value of, which goes to it after the null check
    bool UnwrapsSource = default,
    // What a null source gives: default!, or null for a nullable struct target the result of the mapper, the
    // struct, goes into, as the type of the conditional is that of the result
    string NullResult = "default!",
    bool IsSourceNullable = default,
    // The value goes to the argument of the constructor a return mapper calls, as the parameter assigns the
    // member
    bool IsConstructorArgument = default,
    // The value is made before construction and set in the object initializer of a return mapper, as the member
    // is init-only or required
    bool IsInitializerEntry = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);
