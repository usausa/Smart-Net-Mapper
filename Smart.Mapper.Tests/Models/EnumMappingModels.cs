#pragma warning disable CA1008
namespace Smart.Mapper.Models;

// A2: Enum マッピング用モデル
// A2: Models for enum mapping

// -- enum 定義 --
// -- enum definitions --

public enum SourceStatus
{
    Active = 1,
    Inactive = 2,
    Pending = 3
}

public enum DestStatus
{
    Active,
    Inactive,
    Pending
}

// SourceStatus と一部のメンバーが異なる enum（Unknown のみ共通外）
// An enum whose members partly differ from SourceStatus (only Unknown is outside the shared set)
public enum PartialDestStatus
{
    Active,
    Inactive
}

// -- enum ↔ enum (同名一致) --
// -- enum ↔ enum (same-name match) --

public class EnumToEnumSource
{
    public SourceStatus Status { get; set; }
}

public class EnumToEnumDestination
{
    public DestStatus Status { get; set; }
}

// -- nullable enum ↔ enum --

public class NullableEnumSource
{
    public SourceStatus? Status { get; set; }
}

public class NullableEnumDestination
{
    public DestStatus? Status { get; set; }
}

// -- enum → int --

public class EnumToIntSource
{
    public SourceStatus Status { get; set; }
}

public class EnumToIntDestination
{
    public int Status { get; set; }
}

// -- int → enum --

public class IntToEnumSource
{
    public int Status { get; set; }
}

public class IntToEnumDestination
{
    public DestStatus Status { get; set; }
}

// -- enum → string --

public class EnumToStringSource
{
    public SourceStatus Status { get; set; }
}

public class EnumToStringDestination
{
    public string Status { get; set; } = default!;
}

// -- string → enum --

public class StringToEnumSource
{
    public string Status { get; set; } = default!;
}

public class StringToEnumDestination
{
    public DestStatus Status { get; set; }
}

// -- 部分一致 (SourceStatus → PartialDestStatus, Unknown は default) --
// -- partial match (SourceStatus → PartialDestStatus, Unknown becomes default) --

public class PartialEnumSource
{
    public SourceStatus Status { get; set; }
}

public class PartialEnumDestination
{
    public PartialDestStatus Status { get; set; }
}
