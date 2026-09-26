# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** は Roslyn Incremental Source Generator ベースの高性能オブジェクトマッパーライブラリです。
`[Mapper]` 属性を付与した `static partial` メソッドに対して、プロパティコピーコードをコンパイル時に自動生成します。

## 特徴

- **ゼロオーバーヘッド** - リフレクションを一切使用しない静的コード生成
- **スペシャライズドメソッド方式** - `ConvertTo{TargetType}` 命名規則による直接呼び出し生成（JIT インライン展開と相性良好）
- **メソッド単位の宣言** - `[Mapper]` を個別メソッドに付与するため、通常のヘルパー関数と同じ感覚で扱える
- **カスタムパラメーター透過** - `Map(Src, Dst, TContext ctx)` のような追加引数を、それを宣言した `[MapUsing]`・`Converter`・`[MapCondition]`・`[BeforeMap]`・`[AfterMap]` のメソッドに渡し、`[MapExpression]` の式からも参照できる
- **NativeAOT / トリミング完全対応** - `<IsAotCompatible>true</IsAotCompatible>` 宣言済み・NativeAOT smoke test 通過済み
- **充実した診断** - フェーズ別採番の 35 種（SMP0001〜SMP0501）をコンパイル時に発行

## インストール

```
dotnet add package Usa.Smart.Mapper
```

パッケージには `analyzers/dotnet/cs` 以下にソースジェネレーター DLL が同梱されており、パッケージを参照するだけで自動的にジェネレーターが動作します。追加設定は不要です。

## 対象フレームワーク

| ライブラリ | フレームワーク |
|-----------|--------------|
| `Smart.Mapper` | net10.0, net9.0, net8.0 |
| `Smart.Mapper.Generator` | netstandard2.0 (Roslyn Incremental Source Generator) |

---

## クイックスタート

```csharp
// static partial クラス内でマッパーを定義
internal static partial class ObjectMapper
{
    // void パターン: 既存インスタンスにマッピング
    [Mapper]
    public static partial void Map(Source source, Destination destination);

    // 戻り値パターン: 新規インスタンスを生成して返す
    [Mapper]
    public static partial Destination Map(Source source);
}
```

### 生成コード（void パターン）

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
}
```

### 生成コード（戻り値パターン）

```csharp
public static partial Destination Map(Source source)
{
    var __d = new Destination();
    __d.Id          = source.Id;
    __d.Name        = source.Name;
    __d.Description = source.Description;
    return __d;
}
```

### 拡張メソッドとして定義する

`[Mapper]` メソッドは拡張メソッドとして宣言できます。生成される実装側の宣言にも `this` が付くため、呼び出し側は自然に書けます。

```csharp
public static partial class ObjectMapper
{
    [Mapper]
    public static partial Destination ToDestination(this Source source);
}

var destination = source.ToDestination();
```

---

## 属性リファレンス

### メソッドレベル属性

| 属性 | 説明 |
|------|------|
| `[Mapper]` | マッピングメソッドの指定 |
| `[Mapper(AutoMap = false)]` | 自動マッピングの無効化 |
| `[Mapper(Strict = true)]` | 未マップ destination プロパティを警告（SMP0501） |
| `[Mapper(NameComparison = ...)]` | プロパティ名の比較方式。自動マッピングとマッピング属性に書いた名前の双方に適用（既定: `Ordinal`） |
| `[Mapper(Culture = "...")]` | 型変換時に使用するカルチャ（例: `"ja-JP"`） |
| `[Mapper(DateTimeFormat = "...")]` | `DateTime` <-> `string` 変換時のフォーマット（`Culture` と共に使用） |
| `[Mapper(NumberFormat = "...")]` | 数値型 <-> `string` 変換時のフォーマット（`Culture` と共に使用） |
| `[MapProperty]` | プロパティ間の明示的マッピング。`NullValue`・`NullBehavior`・`Culture`・`DateTimeFormat`・`NumberFormat`・`Converter` 対応 |
| `[MapProperty<T>]` | 型安全版 `[MapProperty]`（C# 11+） |
| `[MapUsing]` | 静的メソッドによる値の計算（カスタムパラメーター対応） |
| `[MapFrom]` | ソースオブジェクトのインスタンスメソッド呼び出し、またはドット記法プロパティパス |
| `[MapConstant]` | 固定値の設定 |
| `[MapConstant<T>]` | 型安全版 `[MapConstant]`（C# 11+） |
| `[MapExpression]` | 任意の C# 式を埋め込む（例: `"System.DateTime.Now"`） |
| `[MapIgnore]` | プロパティのマッピングを除外 |
| `[BeforeMap]` | マッピング前のコールバック |
| `[AfterMap]` | マッピング後のコールバック |
| `[MapCondition]` | 条件メソッドが `true` を返したときだけ destination プロパティをマッピング |
| `[MapCollection]` | 明示的マッパーメソッドを使ったコレクションマッピング。`Strategy`・`Converter` 対応 |
| `[MapNested]` | 明示的マッパーメソッドを使ったネストオブジェクトマッピング |
| `[ValueConverter]` | カスタム型変換器（メソッド / クラスレベル）。`Method` 対応 |
| `[CollectionConverter]` | カスタムコレクション変換器（メソッド / クラスレベル） |

> **第1引数の規則** - destination のメンバーをマッピングする属性では、第1引数は **destination**（ターゲット）名です。第2引数は `[MapProperty]`・`[MapFrom]`・`[MapCollection]`・`[MapNested]` では source、`[MapUsing]`・`[MapCondition]`・`[MapConstant]`・`[MapExpression]` ではメソッド・定数・式です。

### クラスレベル属性

| 属性 | 説明 |
|------|------|
| `[MapperProfile]` | クラス内全 `[Mapper]` メソッドへの既定値設定（`Strict`・`NameComparison`・`Culture`・`DateTimeFormat`・`NumberFormat`）。メソッド側の明示指定が優先 |
| `[ValueConverter]` | クラス内全 `[Mapper]` メソッドへのカスタム型変換器の既定値設定 |
| `[CollectionConverter]` | クラス内全 `[Mapper]` メソッドへのカスタムコレクション変換器の既定値設定 |

---

## 主要機能

### 自動マッピング

同名・互換型のプロパティは自動的にマッピングされます。

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);
```

### プロパティ名変換（`[MapProperty]`）

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]
public static partial void Map(Source source, Destination destination);
```

ソース名は省略可能で、省略時はターゲット名が指定されたものとして扱われます。同名のプロパティに対してオプションだけを指定したい場合に便利です。

```csharp
[Mapper]
[MapProperty(nameof(Destination.Amount), Culture = "en-US", NumberFormat = "C")]
public static partial void Map(Source source, Destination destination);
```

`[MapNested]` / `[MapCollection]` も同じ規則です。

解決できない名前は無視されず診断されます（ソース側は `SMP0213`、ターゲット側は `SMP0214`）。

### 名前の比較方式（`NameComparison`）

`NameComparison` は自動マッピングだけでなく、**マッピング属性に書いた名前**にも適用されます。完全一致が常に優先され、設定した比較方式はフォールバックとしてのみ使われるため、既定（`Ordinal`）の挙動は従来どおりです。

```csharp
public class Src { public int other { get; set; } }
public class Dst { public int Value { get; set; } }

[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
[MapProperty("value", "Other")]   // どちらの綴りも宣言と完全一致していない
public static partial Dst Map(Src src);
```

生成コード（両方とも宣言されたメンバーに解決される）：

```csharp
__d.Value = src.other;
```

`[MapIgnore]` のようにターゲット名のみを取る属性を含め、すべてのマッピング属性が対象です。

### Null 代替値（`NullValue`）

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullValue = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullValue = 0)]
public static partial void Map(Source source, Destination destination);
```

### 写し先の値を残す（`NullBehavior.Skip`）

`NullBehavior.Skip` を指定すると、source が null のときは値を代入せず、destination のメンバーをそのまま残します。

```csharp
// Source: string? Name, int? Count / Destination: string Name, string Count
[Mapper]
[MapProperty(nameof(Destination.Name), NullBehavior = NullBehavior.Skip)]
[MapProperty(nameof(Destination.Count), NullBehavior = NullBehavior.Skip)]
public static partial void Map(Source source, Destination destination);
```

生成コード：

```csharp
if (source.Name is not null)
{
    destination.Name = source.Name!;
}
if (source.Count is not null)
{
    destination.Count = DefaultValueConverter.ConvertToString(source.Count.GetValueOrDefault());
}
```

コンストラクタやオブジェクト初期化子で代入されるメンバーには残すべき値がないため、`NullBehavior.Skip` は指定できません（SMP0215）。

### プロパティ除外（`[MapIgnore]`）

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

### 静的メソッドによる値計算（`[MapUsing]`）

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

カスタムパラメーターも自動で転送されます：

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

`[MapProperty]` の `Converter`、`[MapCondition]`、`[BeforeMap]` / `[AfterMap]` のメソッドも、通常の引数の後にカスタムパラメーターを宣言すれば同じように受け取ります。`[MapExpression]` の式からは名前で参照できます。`[MapCollection]` / `[MapNested]` のマッパーメソッドと、`[ValueConverter]` / `[CollectionConverter]` のクラスのメソッドには渡りません。

### ソースメソッド / プロパティパス（`[MapFrom]`）

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // インスタンスメソッド呼び出し
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // ドット記法パス
public static partial void Map(Source source, Destination destination);
```

### 固定値（`[MapConstant]` / `[MapConstant<T>]`）

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

非 Generic 版: `[MapConstant("Status", "Active")]`
式の場合: `[MapExpression("CreatedAt", "System.DateTime.Now")]`

式は、マッパーの引数を同じ名前で受け取る static ローカル関数としてコンパイルされます。そのため式から引数を参照でき（例: `"source.Price * source.Quantity"`）、`out var` やパターンで宣言した変数がほかの式と衝突しません。

### Before / After Map コールバック

```csharp
[Mapper]
[BeforeMap(nameof(BeforeMapping))]
[AfterMap(nameof(AfterMapping))]
public static partial void Map(Source source, Destination destination);

private static void BeforeMapping(Source source, Destination destination) { /* ... */ }
private static void AfterMapping(Source source, Destination destination) { /* ... */ }
```

### 条件付きマッピング（`[MapCondition]`）

条件メソッドは source の値（とカスタムパラメーター）を受け取り、`true` を返したときだけ destination プロパティに代入されます。

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

### 自動マッピング無効化（`AutoMap = false`）

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]
public static partial void Map(Source source, Destination destination);
// Id のみマッピング。他のプロパティは無視。
```

### 代入の順序（`Order`）

`[MapProperty]`・`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` は `Order` を指定できます。同じ種類の代入は `Order` の昇順（既定は 0）、同じ値なら宣言の順に生成されます。

```csharp
[Mapper(AutoMap = false)]
[MapConstant(nameof(Destination.Label), "second", Order = 2)]
[MapConstant(nameof(Destination.Note), "first", Order = 1)]
public static partial void Map(Source source, Destination destination);
```

生成コード：

```csharp
destination.Note = "first";
destination.Label = "second";
```

種類の順は `[BeforeMap]` と `[AfterMap]` の間で決まっています。プロパティのマッピング（自動マッピングと `[MapProperty]`。source パスの null チェックで囲むものは最後）、`[MapConstant]`、`[MapExpression]`、`[MapUsing]`、`[MapFrom]`、`[MapNested]`、`[MapCollection]` の順です。`Order` で種類をまたいで順を変えることはできません。

---

## ネストプロパティマッピング

`[MapProperty]` のドット記法でネストプロパティの展開・集約が可能です。

### Flatten（ネスト source → フラット destination）

```csharp
[Mapper]
[MapProperty("ChildId",   "Child.Id")]
[MapProperty("ChildName", "Child.Name")]
public static partial void Map(Source source, Destination destination);
```

nullable な中間オブジェクトには null チェックが追加されます：

```csharp
if (source.Child is not null)
{
    destination.ChildId   = source.Child.Id;
    destination.ChildName = source.Child.Name;
}
```

### Unflatten（フラット source → ネスト destination）

```csharp
[Mapper]
[MapProperty("Child1.Value", "Value1")]
[MapProperty("Child2.Value", "Value2")]
public static partial void Map(Source source, Destination destination);
```

destination 側の中間オブジェクトは自動インスタンス化されます：

```csharp
destination.Child1 ??= new DestinationChild();
destination.Child2 ??= new DestinationChild();
destination.Child1.Value = source.Value1;
destination.Child2.Value = source.Value2;
```

---

## コレクションマッピング（`[MapCollection]`）

要素マッパーメソッドの明示的な指定が必要です。

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial DestinationChild MapChild(SourceChild source);

    [Mapper]
    [MapCollection(nameof(Destination.Children), nameof(Source.Children), Mapper = nameof(MapChild))]
    public static partial void Map(Source source, Destination destination);
}
```

生成コード（`List<SourceChild>` から `List<DestinationChild>` の場合）：

```csharp
{
    var __src = CollectionsMarshal.AsSpan(source.Children);
    var __list = new List<DestinationChild>(__src.Length);
    CollectionsMarshal.SetCount(__list, __src.Length);
    var __dst = CollectionsMarshal.AsSpan(__list);
    for (var __i = 0; __i < __src.Length; __i++)
    {
        __dst[__i] = MapChild(__src[__i]);
    }
    destination.Children = __list;
}
```

ループはソースとターゲットのコレクション型に合わせてインラインで生成されます。ソースコレクションが null の場合はターゲットに `default` を代入します。ターゲットは、ループが作るコレクション（`List<T>` とそのインターフェースには `List<T>`、配列、集合には `HashSet<T>`、イミュータブル・フローズンなコレクションにはその型）を受け取れる型である必要があります。`ObservableCollection<T>` のような独自のコレクションクラスは、コレクション変換器で作る場合を除き診断されます（SMP0217）。void の要素マッパー `(SourceChild, DestinationChild)` は `new DestinationChild()` で作ったインスタンスを埋めるため、要素の型は `new()` で作れる必要があります（そうでなければ SMP0210）。

コレクション変換器（後述の `[CollectionConverter]`）を指定すると、ループの代わりにそのメソッドが呼ばれます（例: `CustomCollectionConverter.ToList<SourceChild, DestinationChild>(source.Children, MapChild)!`）。`[MapCollection]` の `Converter` は呼ぶメソッドを指定します。対象は `[CollectionConverter]` の型で、指定がなければ `DefaultCollectionConverter` です。`DefaultCollectionConverter` は関数マッパー・アクションマッパーどちらにも対応したこれらのメソッド（`ToList`・`ToArray`・`ToHashSet`・`ToImmutableArray` など）を提供します。

### 既存のコレクションに詰め直す（`Strategy = CollectionStrategy.InPlace`）

既定ではターゲットに新しいコレクションを代入します。`CollectionStrategy.InPlace` はターゲットのインスタンスを残したまま空にし、写した要素を追加するため、ほかから参照されているインスタンスを保てます。

```csharp
[Mapper(AutoMap = false)]
[MapCollection(nameof(Destination.Children), Mapper = nameof(MapChild), Strategy = CollectionStrategy.InPlace)]
public static partial void Map(Source source, Destination destination);
```

生成コード（`List<SourceChild>` から `List<DestinationChild>` の場合）：

```csharp
{
    if (destination.Children is null)
    {
        destination.Children = new List<DestinationChild>(source.Children.Count);
    }
    destination.Children.Clear();
    destination.Children.EnsureCapacity(source.Children.Count);
    var __srcSpan = CollectionsMarshal.AsSpan(source.Children);
    var __dstColl = destination.Children;
    for (var __i = 0; __i < __srcSpan.Length; __i++)
    {
        __dstColl.Add(MapChild(__srcSpan[__i]));
    }
}
```

ターゲットが null のときは新しい `List<T>`（`HashSet<T>` / `ISet<T>` には `HashSet<T>`）を作るため、ターゲットはそれを受け取れる、セッターのあるプロパティである必要があります。`List<T>`・`IList<T>`・`ICollection<T>`・`IReadOnlyList<T>`・`HashSet<T>`・`ISet<T>` などです（そうでなければ SMP0217、セッターがなければ SMP0212）。インターフェースで宣言したターゲットは `ICollection<T>` として詰めるため、そのインスタンスは変更できるものである必要があります。`InPlace` は常にループを生成し、コレクション変換器は使いません。

---

## 子オブジェクトマッピング（`[MapNested]`）

```csharp
[Mapper]
[MapNested(nameof(Destination.Child), nameof(Source.Child), Mapper = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

生成コード：

```csharp
destination.Child = source.Child is not null ? MapChild(source.Child!) : default!;
```

---

## record / プライマリコンストラクタ対応

destination 型が `record` またはプライマリコンストラクタを持つクラスの場合、ジェネレーターが自動的にコンストラクタ呼び出しを生成します。

パラメータ付きコンストラクタ（宣言されている中で最長のもの）が呼ばれるのは、構築にそれが必要な場合のみです：型が `record` である、settable な対応プロパティを持たないパラメータがある、または public なパラメータレスコンストラクタが存在しない場合。それ以外は `new Dst()` + プロパティ代入（init 専用メンバーはオブジェクト初期化子で代入）が生成されます。void マッパーは構築を行わないため、destination に便宜的なパラメータ付きコンストラクタがあっても影響しません。

```csharp
public record DestModel(int Id, string Name);

[Mapper]
public static partial DestModel Map(SrcModel src);
```

生成コード：

```csharp
public static partial DestModel Map(SrcModel src)
{
    var __d = new DestModel(src.Id, src.Name);
    return __d;
}
```

> `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティなど）に代入できません（SMP0302）。

### コンストラクタ引数の変換

コンストラクタ引数は通常のプロパティ代入と同じ変換パイプラインを通るため、型変換・`Converter`・`NullValue`・`Culture` / フォーマット指定がすべて適用されます。オブジェクト初期化子で代入される `init` 専用メンバーも同様です。

```csharp
public class Src { public int? Value { get; set; } }
public record Dst(string Value);

[Mapper]
public static partial Dst Map(Src src);
```

生成コード：

```csharp
var __d = new Dst(src.Value is not null
    ? DefaultValueConverter.ConvertToString(src.Value.GetValueOrDefault())
    : default!);
```

null 許容ソースが null の場合、引数はターゲット型の `default` になります（`NullValue` 指定時はその値、ターゲットが null 許容なら `null`）。ドット記法ソースパスの null 許容な中間セグメントも同様にガードされます（`src.Child is not null ? ... : default!`）。

文ベースのオプションはコンストラクタ引数には適用できません。`[MapCondition]` はメンバーを未代入のまま残す手段がなく、`NullBehavior.Skip` は保持すべき既存値がないためです。いずれも `SMP0215` で拒否されます。

セッターの無いプロパティをコンストラクタ経由で代入する場合もリネームできます。

```csharp
public class Src { public int Other { get; set; } }

public class Dst
{
    public string Value { get; }
    public Dst(string value) { Value = value; }
}

[Mapper]
[MapProperty(nameof(Dst.Value), nameof(Src.Other))]
public static partial Dst Map(Src src);
```

対応する destination プロパティが存在しないコンストラクタ引数も同様に扱われます。リネームする場合は `[MapProperty]` のターゲットに**引数名**を指定します。

```csharp
public class Src { public int Other { get; set; } }

public class Dst
{
    public Dst(string value) { Text = value; }
    public string Text { get; }
}

[Mapper]
[MapProperty("value", nameof(Src.Other))]   // "value" はコンストラクタ引数名
public static partial Dst Map(Src src);
```

---

## Null 処理

| ソース型 | デスティネーション型 | 動作 |
|----------|---------------------|------|
| `T?` | `T?` | そのままコピー（null も含む） |
| `T?` | `T`（末端） | null の場合 `default!` を代入 |
| `T` | `T?` | そのままコピー |
| `T` | `T` | そのままコピー |

**source 側**の nullable 中間パスには `if (... is not null)` ガードが付きます。
**destination 側**の nullable 中間パスは `??= new` で自動インスタンス化されます。

元の引数（void マッパーでは宛先の引数も）を `Map(Src? source)` のように null 許容で宣言すると、写す前に検査します。null のときは何も写さず、戻り値のあるマッパーは `default` を返し、void マッパーは宛先に触れずに戻ります。

---

## 型変換

同型・暗黙的変換可能な代入はコンバーター不要で直接生成されます。
明示的な変換が必要な場合は `DefaultValueConverter` が使用されます。

### スペシャライズドメソッドパターン

```csharp
// string -> int
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);

// int -> string
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);
```

### Nullable 処理（ジェネレーター側で処理）

```csharp
// int? -> string
destination.StringValue = source.NullableValue is not null
    ? DefaultValueConverter.ConvertToString(source.NullableValue.GetValueOrDefault())
    : default!;
```

### カスタム型変換器（`[ValueConverter]`）

```csharp
public static class CustomConverter
{
    public static string ConvertToString(int source) => $"ID_{source}";
    public static TDestination Convert<TSource, TDestination>(TSource source) { ... }
}

[Mapper]
[ValueConverter(typeof(CustomConverter))]
public static partial void Map(Source source, Destination destination);
```

変換器のクラスは入れ子や総称型でも構いません（`typeof(Outer.CustomConverter)`・`typeof(CustomConverter<TMarker>)`）。`Method` は、メソッドを探す名前を変えます（既定は `"Convert"`）。スペシャライズドメソッドは `{Method}To{TargetType}`、汎用のフォールバックは `{Method}<TSource, TDestination>` になります。

```csharp
public static class MapConverter
{
    public static string MapToString(int source) => $"ID_{source}";
    public static TDestination Map<TSource, TDestination>(TSource source) { ... }
}

[Mapper]
[ValueConverter(typeof(MapConverter), Method = "Map")]
public static partial void Map(Source source, Destination destination);

// 生成コード: destination.Value = MapConverter.MapToString(source.Value);
```

スペシャライズドメソッドのない変換で使う汎用のフォールバックなど、変換器のメソッドが見つからない場合は診断されます（SMP0104）。

優先順位（高 → 低）：

| レベル | 適用範囲 |
|--------|----------|
| `[MapProperty(Converter = nameof(...))]` | 単一プロパティ |
| マッパーメソッドの `[ValueConverter]` | そのメソッドの全プロパティ |
| クラスの `[ValueConverter]` | クラス内の全マッパーメソッド |
| `DefaultValueConverter` | フォールバック |

### カスタムコレクション変換器（`[CollectionConverter]`）

```csharp
public static class CustomCollectionConverter
{
    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source, Func<TSource, TDest> mapper) { ... }
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source, Func<TSource, TDest> mapper) { ... }
}

[Mapper]
[CollectionConverter(typeof(CustomCollectionConverter))]
[MapCollection(nameof(Destination.Items), nameof(Source.Items), Mapper = nameof(MapItem))]
public static partial void Map(Source source, Destination destination);
```

呼ぶメソッドは、`[MapCollection]` の `Converter` で指定しなければターゲットの型で決まり（`ToList`・`ToArray`・`ToHashSet`・`ToImmutableArray` など）、`Method<TSourceElement, TTargetElement>(source, mapper)` として呼ばれます。メソッドがない場合、ソースのコレクションを受け取れない場合、ターゲットのプロパティが受け取れない型を返す場合は診断されます（SMP0104）。

---

## Culture / Format

```csharp
[MapperProfile(Culture = "ja-JP")]
internal static partial class AppMappers
{
    [Mapper(Culture = "de-DE", NumberFormat = "N2")]
    public static partial Dest Map(Src src);

    [Mapper]
    [MapProperty(nameof(Dest2.Amount), nameof(Src2.Price), Culture = "en-US", NumberFormat = "C")]
    public static partial Dest2 Map(Src2 src);
}
```

優先順位: `[MapProperty]` > `[Mapper]` > `[MapperProfile]` > `CultureInfo.InvariantCulture`

カルチャを指定すると、変換器のスペシャライズドメソッドは、カルチャと書式を受け取るオーバーロードで呼ばれます（例: `DefaultValueConverter.ConvertToString(int source, IFormatProvider culture, string? format)`）。独自の `[ValueConverter]` は、使うスペシャライズドメソッドごとにこのオーバーロードを用意する必要があります（そうでなければ SMP0104）。

解決された `CultureInfo` は生成クラス内で `static readonly` フィールドとしてキャッシュされ、変換ごとの `GetCultureInfo(...)` 呼び出しコストを排除します。

> `Culture` なしで `DateTimeFormat` / `NumberFormat` のみ指定するとコンパイルエラー（SMP0401）。

---

## NativeAOT / トリミング対応

Smart.Mapper は NativeAOT および IL トリミングに完全対応しています。

- `Smart.Mapper.csproj` に `<IsAotCompatible>true</IsAotCompatible>` を宣言済み
- すべての型変換はスペシャライズドメソッドで完結 - 実行時のジェネリックリフレクションフォールバックなし
- 生成コードは `Activator.CreateInstance` を使用しない。オブジェクト生成はジェネレーターがインライン展開（要素を `new()` で生成する `DefaultCollectionConverter` の `Action` オーバーロードには `RequiresDynamicCode` を付与）
- `ValueConverterAttribute.ConverterType` と `CollectionConverterAttribute.ConverterType` に `[DynamicallyAccessedMembers]` 注釈付与済み

> **`[MapExpression]` の注意** - 式の中にリフレクション API（`Activator`・`Type.GetType`・`MethodInfo` など）が含まれる場合、SMP0403 が発行されます。AOT 環境では `[MapFrom]` または `[MapUsing]` への置き換えを検討してください。

---

## 診断メッセージ

| コード | 説明 | 重大度 |
|--------|------|--------|
| SMP0001 | マッパーメソッドは `static partial` である必要がある | エラー |
| SMP0002 | マッパーメソッドのパラメーター数が無効 | エラー |
| SMP0003 | カスタムパラメーターの型が重複している | エラー |
| SMP0004 | マッパーメソッドのパラメーター名が `__` で始まっている（生成コードの予約名） | エラー |
| SMP0005 | 生成コードが扱えない修飾子がパラメーターに付いている（`out`、void マッパーの struct の宛先への `in` / `ref readonly`） | エラー |
| SMP0101 | 同一目的プロパティへのマッピングが重複している | エラー |
| SMP0102 | `BeforeMap` メソッドのシグネチャが一致しない | エラー |
| SMP0103 | `AfterMap` メソッドのシグネチャが一致しない | エラー |
| SMP0104 | コンバーターメソッドが見つからない、またはシグネチャが一致しない | エラー |
| SMP0105 | コンバーターの戻り値型が目的プロパティ型と一致しない | エラー |
| SMP0106 | プロパティ条件メソッドのシグネチャが一致しない | エラー |
| SMP0201 | `MapUsing` メソッドのシグネチャが一致しない | エラー |
| SMP0202 | `MapUsing` メソッドの戻り値型が目的プロパティ型と一致しない | エラー |
| SMP0203 | `[MapFrom]` ターゲットプロパティが目的型に存在しない | エラー |
| SMP0204 | `MapFrom` メンバーはソース型の引数なしメソッドまたはプロパティパスである必要がある | エラー |
| SMP0205 | `MapFrom` メンバーの型が目的プロパティ型と一致しない | エラー |
| SMP0206 | `[MapCollection]` / `[MapNested]` のソースプロパティが見つからない | エラー |
| SMP0207 | `[MapCollection]` / `[MapNested]` のターゲットプロパティが見つからない | エラー |
| SMP0208 | `[MapCollection]` のソースプロパティがコレクション型ではない | エラー |
| SMP0209 | `[MapCollection]` のターゲットプロパティがコレクション型ではない | エラー |
| SMP0210 | `MapCollection` 要素マッパーメソッドが見つからないまたはシグネチャが一致しない | エラー |
| SMP0211 | `MapNested` マッパーメソッドが見つからないまたはシグネチャが一致しない | エラー |
| SMP0212 | `[MapCollection]` / `[MapNested]` の対象に代入できない（マッパーから呼べるセッターがない、init 専用、required） | エラー |
| SMP0213 | `[MapProperty]` のソースプロパティが見つからない | エラー |
| SMP0214 | `[MapProperty]` のターゲットプロパティが見つからない、または代入できない | エラー |
| SMP0215 | コンストラクタ / 初期化子経由で代入されるターゲットに `[MapCondition]` / `NullBehavior.Skip` を指定 | エラー |
| SMP0216 | コンストラクタ経由で代入されるメンバーに `[MapIgnore]` を指定 | エラー |
| SMP0217 | `[MapCollection]` の対象が、生成コードの作るコレクションを受け取れない | エラー |
| SMP0301 | コンストラクターパラメーターに一致するソースプロパティがない | エラー |
| SMP0302 | `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティなど）に代入できない | エラー |
| SMP0303 | `required` メンバーがマップされていない | エラー |
| SMP0401 | `Culture` なしで `DateTimeFormat` / `NumberFormat` を指定している | エラー |
| SMP0402 | AOT 非対応: 汎用 `Convert<TSource,TDest>` フォールバックに到達する可能性がある | エラー |
| SMP0403 | AOT 警告: `MapExpression` にリフレクションパターンが含まれる可能性がある | 警告 |
| SMP0501 | Strict モード: マップされていない目的プロパティがある | 警告 |

それぞれの原因と対処は [Diagnostics.md](Diagnostics.md)（英語）を参照してください。

---

## ベンチマーク

[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) を使用して .NET 10 上で計測。

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.300
  [Host] / MediumRun : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
Job=MediumRun  IterationCount=15  LaunchCount=2  WarmupCount=10
```

### 単純マッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 9.391 ns | 0.478 ns | 0.715 ns | 1.01 | 64 B |
| SmartMapper | 9.171 ns | 0.361 ns | 0.529 ns | 0.98 | 64 B |

### 型変換マッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 93.17 ns | 4.364 ns | 6.531 ns | 1.00 | 128 B |
| SmartMapper | 88.73 ns | 3.195 ns | 4.782 ns | 0.96 | 128 B |

### ネストマッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 11.08 ns | 0.390 ns | 0.584 ns | 1.00 | 72 B |
| SmartMapper | 13.68 ns | 1.184 ns | 1.772 ns | 1.24 | 72 B |

### void のネストマッピング（ラムダの除去）

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 9.038 ns | 0.154 ns | 0.231 ns | 1.00 | 72 B |
| LegacyLambda | 9.341 ns | 0.283 ns | 0.424 ns | 1.03 | 72 B |
| SmartMapper | 9.160 ns | 0.395 ns | 0.591 ns | 1.01 | 72 B |

### コレクションマッピング — 要素単位（どちらも `List<T>` を返す）

リストは呼び出し側で管理し、SmartMapper は要素ごとのマッピングにだけ使います。

| Method | ItemCount | Mean | Error | StdDev | Ratio | Allocated |
|--------|----------:|-----:|------:|-------:|------:|----------:|
| Direct | 10 | 101.1 ns | 2.02 ns | 3.98 ns | 1.00 | 456 B |
| SmartMapper | 10 | 107.6 ns | 2.22 ns | 6.40 ns | 1.07 | 456 B |
| Direct | 100 | 812.7 ns | 29.30 ns | 86.40 ns | 1.01 | 4,056 B |
| SmartMapper | 100 | 745.7 ns | 26.04 ns | 75.96 ns | 0.93 | 4,056 B |

### コレクションマッピング — ラッパー単位（どちらも `CollectionWrapper` を返す）

Direct と SmartMapper のどちらも `CollectionWrapper { Items = List<T> }` を作ります。

| Method | ItemCount | Mean | Error | StdDev | Ratio | Allocated |
|--------|----------:|-----:|------:|-------:|------:|----------:|
| Direct | 10 | 114.6 ns | 2.38 ns | 7.02 ns | 1.00 | 512 B |
| SmartMapper | 10 | 112.2 ns | 3.18 ns | 9.39 ns | 0.98 | 512 B |
| Direct | 100 | 891.4 ns | 25.95 ns | 76.51 ns | 1.01 | 4,112 B |
| SmartMapper | 100 | 916.5 ns | 27.90 ns | 82.27 ns | 1.04 | 4,112 B |

> **JIT の分析:**
> - **単純 / 型変換**: 逆アセンブルで、JIT が同一または同等の命令列を生成することを確認しています。型変換で SmartMapper が速いのは、スペシャライズドな `ConvertToString(InvariantCulture)` の経路がボックス化を避けるためです。
> - **ネスト（1.24 倍）**: 逆アセンブルでは、`MapNested` と `MapAddress` を完全にインライン展開したあと、Direct と SmartMapper は同等のコード（155 バイトと 157 バイト）になります。報告された比率はばらつきが大きく（StdDev は Direct の 0.58 ns に対して 1.77 ns、P90 は 11.75 ns に対して 15.84 ns）、コードの質の差ではなく、ループの後方分岐の予測によるノイズと考えられます。
> - **void のネスト**: ラムダを使わない複数文の形（LegacyLambda 1.03 倍 → SmartMapper 1.01 倍）で、クロージャの割り当てによるオーバーヘッドがなくなったことを確認できます。
> - **コレクション**: 要素単位とラッパー単位のどちらでも、SmartMapper は Direct と統計的なノイズの範囲内（比率 0.93〜1.07）です。割り当てはそれぞれ同じです。要素マッパー（`MapItem`）は JIT で完全にインライン展開されます。

---

## テストの実行

### 単体テスト（`Smart.Mapper.Tests`）

xUnit v3 と Microsoft Testing Platform を使用します。

```powershell
# 全テストを実行
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj

# コードカバレッジ付きで実行（出力フォルダーの TestResults に Cobertura XML を出力）
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj -- --coverage --coverage-settings CodeCoverage.runsettings
```

Visual Studio のテストエクスプローラーからも実行できます。

> **注:** .NET 10 SDK では Microsoft Testing Platform と VSTest の非互換により `dotnet test` を使えません。`dotnet run --project` を使ってください。

### ソースジェネレーターテスト（`Smart.Mapper.Generator.Tests`）

Roslyn ソースジェネレーターが正しい出力と診断を生成することを検証します。

```powershell
dotnet run --project Smart.Mapper.Generator.Tests/Smart.Mapper.Generator.Tests.csproj
```

### NativeAOT スモークテスト（`Smart.Mapper.AotTests`）

生成されたマッパーコードが NativeAOT publish 環境で正しく動作することを検証します。

**1. NativeAOT として発行**

```powershell
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64
```

> 対応 RID: `win-x64`・`linux-x64` など。実行環境に合わせて変更してください。

**2. 発行した実行ファイルを実行**

```powershell
.\Smart.Mapper.AotTests\bin\Release\net10.0\win-x64\publish\Smart.Mapper.AotTests.exe
```

**3. 出力を確認**

8 つのシナリオすべてが成功する必要があります：

```
Smart.Mapper AOT smoke tests starting...
  [OK] Basic void mapping
  [OK] Basic return mapping
  [OK] Type conversion
  [OK] Enum mapping
  [OK] Null handling
  [OK] Nested property mapping
  [OK] Collection mapping
  [OK] Custom value converter
All AOT smoke tests passed.
```

テストが失敗した場合、プロセスは非ゼロの終了コードで終了し、標準エラーに `FAIL: <message>` が出力されます。

**4. AOT 警告の確認（任意）**

```powershell
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64 2>&1 |
    Select-String "IL2|IL3"
```

`IL2xxx` / `IL3xxx` 診断が出力されないことを確認します。

---

## TODO

将来的に改善を検討している項目:

- **`[MapCollection]` / `[MapNested]` の init-only / required メンバー対応** — 生成されるループが構築後に実行されるため、現在は `SMP0212` で拒否している。構築前にローカルへコレクション / ネストインスタンスを構築し、オブジェクト初期化子で代入する方式で対応可能。
- **`FrozenSet` の直接構築** — 生成コードは `HashSet<T>` を構築してから `ToFrozenSet` を呼ぶ（BCL の設計上の二段構築）。BCL に frozen コレクションのビルダー API が追加されれば、中間セットを排除できる。
- **ジェネリックフォールバック `Convert<TSource, TDestination>` の `Half` / `Int128` / `UInt128` / `BigInteger` ソース対応** — ジェネリックコンバーターへのオプトイン経由では boxing フォールバックに到達する。既定の specialized メソッド経路はカバー済みのため、需要が生じた場合に分岐を追加する。
- **ジェネレーターのインクリメンタリティ調整** — 出力は `Collect()` 経由で実行ごとに再生成され、プロパティ走査も機能パスごとに繰り返される。現状の実測コストは無視できる水準のため、非常に大きなモデルが現れた場合に再検討する（クラス単位の出力分割・プロパティリストのキャッシュ）。

---

## ライセンス

MIT
