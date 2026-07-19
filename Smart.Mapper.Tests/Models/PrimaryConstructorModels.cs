namespace Smart.Mapper.Models;

// Record types (D1)
public record RecordSource(int Id, string Name, int Age);

public record RecordDestination(int Id, string Name, int Age);

public record RecordDestinationPartial(int Id, string Name);

// Class with primary constructor (D3)
public class PrimaryCtorSource
{
    public int Id { get; init; }
    public string Name { get; init; } = default!;
    public int Age { get; init; }
}

public class PrimaryCtorDestination(int id, string name, int age)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public int Age { get; } = age;
}

// Record with extra settable property
public record RecordWithExtra(int Id, string Name)
{
    public string? Extra { get; set; }
}

public class RecordWithExtraSource
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Extra { get; set; }
}

// Source for [MapProperty] override test
public class MapPropertyOverrideSource
{
    public int Identifier { get; set; }
    public string FullName { get; set; } = default!;
}

public record MapPropertyOverrideDestination(int Id, string Name);

// コンストラクタ引数に対する型変換 / Converter / NullValue の適用を検証するためのモデル。
// Models used to verify type conversion, Converter and NullValue on constructor arguments.
public class CtorConversionSource
{
    public int Value { get; set; }

    public int Raw { get; set; }

    public int? Quantity { get; set; }
}

public record CtorConversionDestination(string Value, string Raw, int Quantity);

// セッターの無いプロパティをパラメータ付きコンストラクタで代入するケース。
// A get-only property assigned through a parameterized constructor.
public class CtorGetOnlySource
{
    public int Other { get; set; }
}

public sealed class CtorGetOnlyDestination
{
    public CtorGetOnlyDestination(string value)
    {
        Value = value;
    }

    public string Value { get; }
}

// null 許容ソース + 型変換をコンストラクタ引数で受けるケース。null はターゲット型の default になる。
// A nullable source needing conversion, received as a constructor argument. Null yields the
// destination type's default.
public class CtorNullableConversionSource
{
    public int? Value { get; set; }

    public string? Text { get; set; }
}

public record CtorNullableConversionDestination(string Value, int Text);

// null 許容な中間セグメントを持つドット記法ソースをコンストラクタ引数で受けるケース。
// A dotted source with a nullable intermediate segment received as a constructor argument.
public class CtorNestedGuardSourceChild
{
    public int Val { get; set; }
}

public class CtorNestedGuardSource
{
    public CtorNestedGuardSourceChild? Child { get; set; }
}

public record CtorNestedGuardDestination(string Value);

// コンストラクタ引数に対応する destination プロパティが存在しないケース。
// A constructor parameter with no backing destination property.
public class CtorNoPropertySource
{
    public int Other { get; set; }
}

public sealed class CtorNoPropertyDestination
{
    public CtorNoPropertyDestination(string value)
    {
        Text = value;
    }

    public string Text { get; }
}
