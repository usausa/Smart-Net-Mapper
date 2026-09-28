namespace Smart.Mapper.Generator.Models;

using SourceGenerateHelper;

// Represents a MapFrom mapping (target property set from source expression - method call or property path).
internal sealed record MapFromModel(
    // Target member
    string TargetName = default!,
    // Source. A method name or a property path on the source object
    string Member = default!,
    // Emit order. Order is the attribute's Order, DefinitionOrder is the declaration sequence and breaks ties
    int Order = default,
    int DefinitionOrder = default,
    // Member is a method call rather than a property path
    bool IsMethodCall = default,
    // The intermediate members of the property path that may be null (Customer and Customer.Address for
    // Customer.Address.City), under whose null check the value is read, as for the source path of a property
    // mapping. A nullable reference going to a target not annotated as one is taken with !, and a target that
    // takes null gets null for a null intermediate member where the value is an expression
    EquatableArray<string> NullCheckedPaths = default,
    bool ForgivesNull = default,
    bool IsTargetNullable = default,
    // Target member traits. Decide object-initializer entry vs plain assignment
    bool IsTargetInitOnly = default,
    bool IsTargetRequired = default,
    // The value goes to the argument of the constructor a return mapper calls, as the parameter assigns the
    // member
    bool IsConstructorArgument = default,
    // The attribute this was declared by, as its index in MapperMethodModel.AttributeLocations, which the
    // diagnostics about it point to; -1 for one the automatic mapping made
    int AttributeIndex = -1);
