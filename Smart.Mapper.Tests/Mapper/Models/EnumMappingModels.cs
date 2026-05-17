namespace Smart.Mapper;

// A2: Enum マッピング用モデル

// -- enum 定義 --

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
public enum PartialDestStatus
{
    Active,
    Inactive
}

// -- enum ↔ enum (同名一致) --

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
    public string Status { get; set; } = string.Empty;
}

// -- string → enum --

public class StringToEnumSource
{
    public string Status { get; set; } = string.Empty;
}

public class StringToEnumDestination
{
    public DestStatus Status { get; set; }
}

// -- 部分一致 (SourceStatus → PartialDestStatus, Unknown は default) --

public class PartialEnumSource
{
    public SourceStatus Status { get; set; }
}

public class PartialEnumDestination
{
    public PartialDestStatus Status { get; set; }
}
