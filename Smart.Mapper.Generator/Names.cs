namespace Smart.Mapper.Generator;

// Well-known names the generator matches symbols against or writes into generated source.
//
// They live here rather than on the class that happens to use them first because they are shared:
// the attribute names drive both model building and the diagnostic suppressor, and the converter
// type names are consumed by the emitters. Deriving the generic and global:: variants from their
// base constant keeps the spellings from drifting apart when an attribute is renamed.
internal static class Names
{
    private const string Namespace = "Smart.Mapper.";
    private const string Global = "global::";

    // Attribute metadata names, compared against AttributeClass.ToDisplayString().
    public const string MapperAttribute = Namespace + "MapperAttribute";
    public const string MapperProfileAttribute = Namespace + "MapperProfileAttribute";
    public const string MapPropertyAttribute = Namespace + "MapPropertyAttribute";
    public const string MapIgnoreAttribute = Namespace + "MapIgnoreAttribute";
    public const string MapConstantAttribute = Namespace + "MapConstantAttribute";
    public const string MapExpressionAttribute = Namespace + "MapExpressionAttribute";
    public const string MapConditionAttribute = Namespace + "MapConditionAttribute";
    public const string MapUsingAttribute = Namespace + "MapUsingAttribute";
    public const string MapFromAttribute = Namespace + "MapFromAttribute";
    public const string MapCollectionAttribute = Namespace + "MapCollectionAttribute";
    public const string MapNestedAttribute = Namespace + "MapNestedAttribute";
    public const string BeforeMapAttribute = Namespace + "BeforeMapAttribute";
    public const string AfterMapAttribute = Namespace + "AfterMapAttribute";
    public const string ValueConverterAttribute = Namespace + "ValueConverterAttribute";
    public const string CollectionConverterAttribute = Namespace + "CollectionConverterAttribute";

    // Generic variants. MapProperty is matched on its OriginalDefinition, which renders the type
    // parameter; MapConstant is matched by prefix because its argument type varies.
    public const string MapPropertyAttributeGeneric = MapPropertyAttribute + "<T>";
    public const string MapConstantAttributeGenericPrefix = MapConstantAttribute + "<";

    // Runtime helper types. The plain name is used to look the symbol up, the qualified one is
    // written into the generated source.
    public const string DefaultValueConverter = Namespace + "DefaultValueConverter";
    public const string QualifiedDefaultValueConverter = Global + DefaultValueConverter;
    public const string QualifiedDefaultCollectionConverter = Global + Namespace + "DefaultCollectionConverter";
}
