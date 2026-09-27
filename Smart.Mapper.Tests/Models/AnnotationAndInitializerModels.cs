#pragma warning disable CA1002
#pragma warning disable CA2227
namespace Smart.Mapper.Models;

// A converter and a condition whose parameter does not take null, and a [MapUsing] method returning a nullable
// reference
public class NonNullSource
{
    public string? Name { get; set; }

    public string? Code { get; set; }

    public string? Note { get; set; }
}

public class NonNullDestination
{
    public string Name { get; set; } = "keep";

    public string Code { get; set; } = "keep";

    public string Note { get; set; } = "keep";

    public string Found { get; set; } = "keep";
}

public record NonNullRecord(string Name, string? Code);

// Nullable reference elements going to an element mapper whose parameter does not take null
public class ReferenceElementItem
{
    public int Value { get; set; }
}

public class ReferenceElementItemDto
{
    public int Value { get; set; }
}

public class ReferenceElementSource
{
    public List<ReferenceElementItem?> Items { get; set; } = [];
}

public class ReferenceElementDestination
{
    public List<ReferenceElementItemDto?> Items { get; set; } = [];
}

// Init-only and required targets of [MapNested] / [MapCollection], set in the object initializer
public class InitTargetSource
{
    public ReferenceElementItem? Head { get; set; }

    public List<ReferenceElementItem>? Items { get; set; }
}

public class InitTargetDestination
{
    public ReferenceElementItemDto? Head { get; init; }

    public required IReadOnlyList<ReferenceElementItemDto> Items { get; init; }
}

// A converter returning a type the target converts to implicitly, and a value the target takes by an implicit
// reference conversion
public class WideningShape
{
    public int Size { get; set; }
}

public class WideningCircle : WideningShape
{
}

public class WideningConversionSource
{
    public string Price { get; set; } = "0";

    public IReadOnlyList<WideningCircle> Circles { get; set; } = [];
}

public class WideningConversionDestination
{
    public decimal? Price { get; set; }

    public IReadOnlyList<WideningShape> Circles { get; set; } = [];
}

// A value no member of the target enum has the name of, going to a nullable target
public enum UnmatchedSourceKind
{
    First,
    Second,
    Third
}

public enum UnmatchedTargetKind
{
    First,
    Second
}

public class UnmatchedEnumSource
{
    public UnmatchedSourceKind Kind { get; set; }

    public string Text { get; set; } = string.Empty;
}

public class UnmatchedEnumDestination
{
    public UnmatchedTargetKind? Kind { get; set; }

    public UnmatchedSourceKind? Text { get; set; }
}

// InPlace into a dictionary interface
public class InPlaceDictionarySource
{
    public Dictionary<string, ReferenceElementItem> Items { get; set; } = [];
}

public class InPlaceDictionaryDestination
{
    public IDictionary<string, ReferenceElementItemDto>? Items { get; set; }
}

// A converter, a condition and a [MapUsing] method taking the value as an interface, and a nullable struct going to a
// method taking the struct it holds
public interface IConversionNamed
{
    string Name { get; }
}

public class ConversionPerson : IConversionNamed
{
    public string Name { get; set; } = string.Empty;
}

public class ParameterConversionSource : IConversionNamed
{
    public string Name { get; set; } = string.Empty;

    public ConversionPerson? Owner { get; set; }

    public List<string> Tags { get; set; } = [];

    public int? Quantity { get; set; }

    public int? Score { get; set; }
}

public class ParameterConversionDestination
{
    public string Title { get; set; } = "keep";

    public string OwnerName { get; set; } = "keep";

    public string TagText { get; set; } = "keep";

    public string QuantityText { get; set; } = "keep";

    public string ScoreText { get; set; } = "keep";

    public int? Score { get; set; } = -1;
}

public sealed record ParameterConversionRecord(string QuantityText, string? ScoreText);

// Methods found in a base class of the mapper class and in the class containing it, a callback taking the source and
// the destination as interfaces, and converters taking the value by boxing, a wider number or a user-defined conversion
public interface ILookupEntity
{
    int Id { get; }
}

public interface ILookupStamped
{
    string Stamp { get; set; }
}

public class LookupChild
{
    public int Id { get; set; }
}

public class LookupChildDto
{
    public int Id { get; set; }
}

public class LookupSource : ILookupEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public int? Stock { get; set; }

    public UserId Owner { get; set; }

    public int Total { get; set; }

    public decimal Amount { get; set; }

    public LookupChild Child { get; set; } = new();

    public int? Reserve { get; set; }

    public UserId? Keeper { get; set; }

    public int Shared { get; set; }
}

public class LookupDestination : ILookupStamped
{
    public int Id { get; set; }

    public string Name { get; set; } = "keep";

    public string Count { get; set; } = "keep";

    public string Stock { get; set; } = "keep";

    public string Owner { get; set; } = "keep";

    public string Total { get; set; } = "keep";

    public string Amount { get; set; } = "keep";

    public LookupChildDto Child { get; set; } = new();

    public string Stamp { get; set; } = string.Empty;

    public string Reserve { get; set; } = "keep";

    public string Keeper { get; set; } = "keep";

    public string Shared { get; set; } = "keep";
}

// An update giving no value leaves a null intermediate member of a dotted target null
public class PatchAddress
{
    public string City { get; set; } = string.Empty;

    public string? Zip { get; set; }
}

public class PatchCustomerRequest
{
    public string? City { get; set; }

    public string? Zip { get; set; }
}

public class PatchCustomer
{
    public PatchAddress? Address { get; set; }
}
