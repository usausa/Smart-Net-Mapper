# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** は Roslyn Incremental Source Generator ベースの高性能オブジェクトマッパーライブラリです。
`[Mapper]` 属性を付与した `static partial` メソッドに対して、プロパティコピーコードをコンパイル時に自動生成します。

## 特徴

- **ゼロオーバーヘッド** - リフレクションを一切使用しない静的コード生成
- **スペシャライズドメソッド方式** - `ConvertTo{TargetType}` 命名規則による直接呼び出し生成（JIT インライン展開と相性良好）
- **メソッド単位の宣言** - `[Mapper]` を個別メソッドに付与するため、通常のヘルパー関数と同じ感覚で扱える
- **カスタムパラメーター透過** - `Map(Src, Dst, TContext ctx)` のような追加引数を、それをすべて同じ順で宣言した `[MapUsing]`・`Converter`・`[MapCondition]`・`[BeforeMap]`・`[AfterMap]` のメソッドに渡し、`[MapExpression]` の式からも参照できる
- **NativeAOT / トリミング完全対応** - `<IsAotCompatible>true</IsAotCompatible>` 宣言済み・NativeAOT smoke test 通過済み
- **充実した診断** - フェーズ別採番の 49 種（SMP0001〜SMP0503）をコンパイル時に発行

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

構造体の destination は、`Map(Source source, ref Destination destination)` のように `ref` で受け取ります。値渡しでは呼び出し元に見えないコピーを埋めることになり、`in` や `ref readonly` ではメンバーに代入できないためです（SMP0005）。

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

マッパーはオブジェクトを別のオブジェクトへ写し、作った destination を値で返します。`ref` や `ref readonly` で返すと宣言したものは診断され（SMP0008）、source や destination がコレクション・配列・タプルのものも診断されます（SMP0007。[コレクションマッピング](#コレクションマッピングmapcollection)を参照）。void マッパーは `static partial void Map(Source source, Destination destination);` のようにアクセシビリティ修飾子なしで宣言でき（暗黙に `private`）、実装も同じ形で宣言します。実装は宣言の修飾子（アクセシビリティ・`new`・`unsafe`）を繰り返します。

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

### 入れ子の型の中のマッパー

`[Mapper]` メソッドは入れ子の型の中に宣言できます。生成コードは含む型を外側から順に、種類（`class`・`struct`・`record`・`record struct`）と型パラメーターを合わせて宣言し直すため、どの型も `partial` で、ほかのファイルから宣言できない `file` の型でない必要があります（そうでなければ SMP0001）。

```csharp
public static partial class Mappers
{
    public static partial class Orders
    {
        [Mapper]
        public static partial OrderDto Map(Order source);
    }
}

// 生成コード:
partial class Mappers
{
    partial class Orders
    {
        public static partial OrderDto Map(Order source) { ... }
    }
}
```

### ジェネリックのマッパーメソッド

`[Mapper]` メソッドはジェネリックにできます。生成される実装は型パラメーターとその制約を繰り返し、ソースや destination に使う型パラメーターは、制約の型のプロパティを持つものとして扱います。

```csharp
public static partial class Mappers
{
    [Mapper]
    public static partial PageDto<T> Map<T>(Page<T> source);

    [Mapper]
    public static partial T Create<T>(EntitySource source) where T : Entity, new();
}
```

destination の型パラメーターは `new T()` で作るため、`new()` か `struct` の制約が必要です（そうでなければ SMP0305）。

---

## 属性リファレンス

### メソッドレベル属性

| 属性 | 説明 |
|------|------|
| `[Mapper]` | マッピングメソッドの指定 |
| `[Mapper(AutoMap = false)]` | 自動マッピングの無効化 |
| `[Mapper(Strict = true)]` | 未マップ destination プロパティ（SMP0501）、null を受け付けないターゲットへの null になりうる値（SMP0502）、ターゲットの enum に同じ名前のメンバーがない enum のメンバー（SMP0503）を警告 |
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

> **キーワードの名前** - `@class` のように C# のキーワードである名前は、生成コードでも `@` を付けて書かれます。そのような名前のメンバー・enum のメンバー・引数・メソッド・クラス・名前空間も、ほかと同じようにマッピングできます。

### クラスレベル属性

| 属性 | 説明 |
|------|------|
| `[MapperProfile]` | クラス内全 `[Mapper]` メソッドへの既定値設定（`Strict`・`NameComparison`・`Culture`・`DateTimeFormat`・`NumberFormat`）。メソッド側の明示指定が、設定ごとに優先 |
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

マッパーから代入できないプロパティ（get だけ、`private set` のようにマッパーから呼べないセッター）は、構築で呼ぶコンストラクタが受け取るものを除き、対象から外します。マッピング属性で指定した場合は診断されます（SMP0214）。

型が継承するプロパティも対象です。基底クラスのもの、インターフェイスならそれが継承するインターフェイスのものを含みます。名前は、生成コードの `x.Name` が指すもの、つまりマッパーのクラスからアクセスできる、その名前の最も派生したメンバーとして扱います。基底のプロパティを override したものや `new` で隠したものがその名前を表し、getter だけを override したものには、継承した setter で代入します。名前が指すメンバーが public のインスタンスプロパティでない（`internal` のプロパティ、フィールド、メソッド）なら、その名前は自動では写さず、隠された基底のプロパティも写しません。`private` のメンバーはマッパーのクラスから見えないため、何も隠しません。インターフェイスが、互いに隠さない 2 つのインターフェイスから同じ名前を継承していると、その名前はあいまいなため写しません。インデクサーは対象外です。マッピング属性に書いた名前も同じです。ソースのプロパティは getter で読むため、マッパーのクラスから呼べる getter がないもの（`private get` や set だけのもの）はソースになりません。

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

`[MapNested]` / `[MapCollection]` も同じ規則です。これらのソースはソースの型のプロパティで、ドット付きのパスにはできません（SMP0206）。

解決できない名前は無視されず診断されます（ソース側は `SMP0213`、ターゲット側は `SMP0214`）。`[MapIgnore]`・`[MapCondition]` のターゲットも、存在しなければ同じく診断されます。ターゲットは destination のプロパティやフィールド、そのドット付きパス（`[MapCondition]` のみ）、または戻り値のあるマッパーが呼ぶコンストラクタの引数です。

ある属性でメンバー全体を、別の属性でその中をドット付きパスでマッピングすると（`Child` と `Child.Value`）、両方は成り立たないため、同じターゲットへのマッピングとして診断されます（SMP0101）。

### 名前の比較方式（`NameComparison`）

`NameComparison` は自動マッピングだけでなく、**マッピング属性に書いた名前**にも適用されます。比較方式は `[Mapper]` の指定、なければ `[MapperProfile]` の指定、どちらもなければ `Ordinal` です。完全一致が常に優先され、設定した比較方式はフォールバックとしてのみ使われるため、既定（`Ordinal`）の挙動は従来どおりです。

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

マッピング属性に書いたメンバーの名前はすべて対象です。ターゲット（プロパティもフィールドも、ドット付きパスの各段も。`[MapIgnore]` のようにターゲット名のみを取る属性を含む）、ソース、`[MapFrom]` のメンバーです。大文字小文字を無視して複数が当たるときは、先に宣言されたもの（プロパティ、次にフィールド）が選ばれます。メソッドの名前（`Converter`・`[MapCondition]`・`[MapUsing]`・`[BeforeMap]` / `[AfterMap]`・`[MapCollection]` / `[MapNested]` のマッパー）は C# の識別子として完全一致で探します。

### Null 代替値（`NullValue`）

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullValue = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullValue = 0)]
public static partial void Map(Source source, Destination destination);
```

値は `[MapConstant]` の値と同じように書かれ、`source.Count ?? 0` のようにターゲットの型に変換できる必要があります（そうでなければ SMP0218）。`NullValue = null` には null を受け取れるターゲットが必要です。

null 許容注釈を無効にして宣言した参照型も null を持ちうるため、`NullValue` と `NullBehavior.Skip` は、null 許容として宣言したものと同じように適用します。

source をそのまま受け取る `Converter` と組み合わせたときは、source が null なら `NullValue` を入れ、値があるときだけ変換メソッドを呼びます（`source.Count is not null ? ToText(source.Count) : "none"`）。ドット付きのソースで途中のメンバーが null のときも `NullValue` を入れます（ネストプロパティマッピングを参照）。

引数が null を受け付けない `Converter`（null 許容でない注釈の参照型、または `[DisallowNull]`）にも null の source は渡しません。値があるときだけ呼び、source が null なら `NullValue` を入れます。`NullValue` がなければ、代入ではターゲットをそのまま残し、コンストラクタの引数やオブジェクト初期化子の項目では、null を受け取るターゲットには `null`、それ以外には `default` を渡します。引数が null を受け付ける `Converter`（`string?`、`[AllowNull]`、null 許容注釈を無効にして宣言したもの）には、source をそのまま渡します。null 許容の構造体の source を、中の構造体（`int?` なら `int`）や、中の値から暗黙に変換できる型（`long` や `double`）で受け取る `Converter` には、同じように値があるときだけ `Value` を渡します（`source.Count is not null ? ToText(source.Count.Value) : "none"`）。

```csharp
// Source: string? Name / Destination: string Name
// [MapProperty(nameof(Destination.Name), Converter = nameof(Trim))]、private static string Trim(string value)
if (source.Name is not null)
{
    destination.Name = Trim(source.Name);
}
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

source をそのまま受け取る `Converter` も、値があるときだけ呼びます。

コンストラクタやオブジェクト初期化子で代入されるメンバーには残すべき値がないため、`NullBehavior.Skip` は指定できません（SMP0215）。

### プロパティ除外（`[MapIgnore]`）

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

ターゲットは destination のメンバー全体です。ドット付きのターゲット（`Child.Value`）は診断されます（SMP0223）。自動マッピングはメンバーの中のメンバーを単独で代入しないため、除外するものがありません。

`[MapIgnore]` と、同じターゲットをマッピングする属性は矛盾するため、属性の種類を問わず診断されます（SMP0101）。メンバーを `[MapIgnore]` で除外し、その中へドット付きパスでマッピングするのは許されます。メンバーは自動マッピングから外れ、パスはマッピングされます。

戻り値のあるマッパーのコンストラクタの引数が代入するメンバーや、同じ名前のメンバーがない引数も除外できます。その引数には値がないため、別のコンストラクタを選ぶか、省略可能な引数なら渡さず、引数なしで作れる型なら引数なしで作ります（[コンストラクタの選び方](#コンストラクタの選び方)）。ほかに作る方法がない destination は診断されます（SMP0216）。

戻り値のあるマッパーが作る destination の `required` メンバーは、構築時に設定する必要があるため除外できません（SMP0216）。呼ぶコンストラクタに `[SetsRequiredMembers]` があれば除外できます。

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

`[MapProperty]` の `Converter`、`[MapCondition]`、`[BeforeMap]` / `[AfterMap]` のメソッドも、通常の引数の後にカスタムパラメーターを宣言すれば同じように受け取ります。`[MapExpression]` の式からは名前で参照できます。受け取るメソッドは、カスタムパラメーターをすべて、マッパーが宣言した順に、それぞれの型で宣言します。一部だけを宣言したものや、順番の違うものは一致しません。`[MapCollection]` / `[MapNested]` のマッパーメソッドと、`[ValueConverter]` / `[CollectionConverter]` のクラスのメソッドには渡りません。

これらの属性が指すメソッド（`[MapUsing]` のメソッド、コンバーター、条件、コールバック、`[MapCollection]` / `[MapNested]` のマッパー）は、生成コードが修飾なしの名前で呼ぶ static メソッドです。そのため、C# が呼び出しの名前を探すのと同じように探します。マッパーのクラスとその基底クラス（マッパーのクラスから呼べる `protected` のメソッドを含む）を探し、どちらにも呼び出せるその名前のメンバーがなければ、マッパーのクラスを含むクラスとその基底クラス、さらにその外側と、順に探します。最後に、生成コードのファイルからも見える `global using static` で取り込んだ型を探します（1 つのファイルだけの `using static` は見えません。取り込んだ型からは、その型で宣言したメソッドだけを使い、継承したものや拡張メソッドは使いません）。C# と同じく、マッパーのクラスから使え、呼び出せるその名前のメンバーを持つ最初のクラスだけを探します。そのメソッドが引数を受け取れないときも、外側は探しません。呼び出せないメンバー（デリゲートでないプロパティやフィールド、入れ子の型）は飛ばします。派生クラスのメソッドは、同じシグネチャの基底クラスのメソッドを隠します。インスタンスメソッドは使いません。また型引数なしで呼ぶため、ジェネリックメソッドは使いません。一致しないメソッドとして診断されます。

```csharp
public abstract class MapperBase
{
    protected static string FormatMoney(decimal value) => value.ToString("N2");
}

public static partial class Mappers
{
    private static string Upper(string value) => value.ToUpperInvariant();

    public partial class Orders : MapperBase
    {
        [Mapper]
        [MapProperty(nameof(OrderDto.Total), Converter = nameof(FormatMoney))]  // 基底クラスのメソッド
        [MapProperty(nameof(OrderDto.Name), Converter = nameof(Upper))]         // 外側のクラスのメソッド
        public static partial OrderDto Map(Order source);
    }
}
```

複数のマッパーのクラスで共通に使うメソッドは、`global using static` で取り込むクラス（プロジェクトの 1 つのファイルに `global using static MyApp.Converters;`）か、マッパーのクラスが継承する基底クラスに置けます。プロパティごとに名前で指すのではなく、元とターゲットの型で決まる変換は、`[ValueConverter]` のクラスに置きます（カスタム型変換器を参照）。

`[MapUsing]` のメソッド、`[MapProperty]` の `Converter`、`[MapCondition]` のメソッドは、値（`[MapUsing]` はソース、ほかはソースのメンバー）をその型のまま受け取るほか、値渡しの引数なら、C# が渡すのと同じく暗黙に変換できる型としても受け取れます。基底クラスやインターフェイス（`Person` のソースに `private static string Label(IHasName x)`、`List<string>` のメンバーに `IEnumerable<string>` を受け取るコンバーター）、`object` や構造体が実装するインターフェイス（ボックス化。`DateTime` に `IFormattable`）、より広い数値（`int` に `long`）、null 許容の構造体（`int` に `int?`）、ユーザー定義の暗黙の変換（エラー扱いの `[Obsolete]` のものは除く）です。`in` の引数は、値の型そのものだけを受け取ります。null 許容の構造体は、中の構造体（`int?` なら `int`）を受け取るコンバーターや条件、または値渡しで、中の値から暗黙に変換できる型（`long` や `double`、ユーザー定義の変換）を受け取るものに `Value` を渡します。null を受け付けない引数と同じく、値があるときだけ渡します（Null 代替値と条件付きマッピングを参照）。中の構造体そのものを受け取るものが先で、次に null 許容の構造体を変換して受け取るもの（`long?` や `object`）、最後に中の値を変換して受け取るものです。ユーザー定義の変換を通して渡す値も、変換演算子の引数が null を受け付けなければ、値があるときだけ渡します。基底クラスから派生クラスへのような、明示的な変換でしか値を受け取れないメソッドは一致せず、診断されます（SMP0104、SMP0106、SMP0201）。

オーバーロードは、C# が呼び出しを結び付けるものを使います。値の型を受け取るものが先で、なければ引数の型がもっとも具体的なもの（基底クラスやインターフェイスより、それを継承したクラス。`int` には `object` より `long`）です。選んだメソッドに呼び出しが結び付かないときは、結び付く方を使います（値を受け取れる派生クラスのメソッドがあると、基底クラスのメソッドは候補から外れます。値渡しのものは `in` のものより先です）。あいまいな呼び出し（CS0121）や、一致しないメソッド（明示的な変換で値を受け取るもの、ジェネリックメソッド、省略可能な引数や `params` のあるもの、エラー扱いの `[Obsolete]` のもの）に結び付く、または結び付きうる呼び出しは、一致しないメソッドとして診断されます。結び付くメソッドが別の型を返すときは、戻り値の型で診断されます（SMP0105、SMP0202。条件が `bool` を返さないときは SMP0106）。

`[MapUsing]` のメソッドが返す型は、ターゲットの型か、代入と同じく C# が暗黙に変換できる型です。`long` や `int?` のターゲットに `int`、基底クラスや実装するインターフェイスのターゲットにそのクラスを返せます。`int` のターゲットに `long` のように、明示的な変換が要る型は診断されます（SMP0202）。null 許容の参照を返し、ターゲットが null 許容でないときは、`[MapFrom]` と同じく `!` を付けて受け取ります。ただし、最初の引数を指定した `[return: NotNullIfNotNull]` で、マッパーの null 検査を通った source には null でない値を返すと分かるときは、そのまま受け取ります。

### ソースメソッド / プロパティパス（`[MapFrom]`）

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // インスタンスメソッド呼び出し
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // ドット記法パス
public static partial void Map(Source source, Destination destination);
```

メソッドは、マッパーのクラスから呼べる引数なしのインスタンスメソッドです（ジェネリックメソッドは除きます）。`source.Method()` の呼び出しと同じように探すため、基底クラスのもの、ソースがインターフェイスならそれが継承するインターフェイスのものも見つかります。派生側のものが先で、`new` で隠したメソッドは派生側が使われます。プロパティパスも、継承したプロパティを同じようにたどります。

メンバーの型は、ターゲットの型か、`[MapUsing]` のメソッドと同じく暗黙に変換できる型です（そうでなければ SMP0205）。null になりうるメンバーを通るプロパティパスは、`[MapProperty]` のソースのパスと同じく、その null 検査の下で読みます（null 許容の構造体は中の構造体を通して読みます。`Location.Lat` なら `source.Location.Value.Lat`）。途中が null のときはターゲットをそのまま残し、コンストラクタの引数やオブジェクト初期化子の項目では、null を受け取るターゲットには `null`、それ以外には `default` を渡します。null 許容の参照を null 許容でないターゲットに写すときは、`[MapProperty]` と同じく `!` を付けます。

```csharp
// [MapFrom(nameof(Destination.City), "Customer.Address.City")]、Customer と Address は null 許容
if (source.Customer is not null && source.Customer.Address is not null)
{
    destination.City = source.Customer.Address.City;
}
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

定数はその型の式として書かれます。enum はメンバー（フラグの組み合わせのようにメンバーのない値はキャスト）、型は `typeof`、配列はマッピングごとに作る新しい配列、数値・文字・文字列はどのカルチャでも同じ綴りの C# リテラルです（`double.NaN` や無限大は名前で書き、引用符や制御文字はエスケープします）。リテラルを持たない `byte`・`sbyte`・`short`・`ushort` はキャストで書くため、どんな値も受け取るターゲットでも型を保ちます（`object` には `int` ではなく `short` として箱詰めされます）。

```csharp
[MapConstant(nameof(Destination.Kind), Kind.Active)]                  // __d.Kind = global::Sample.Kind.Active;
[MapConstant(nameof(Destination.Access), Access.Read | Access.Write)] // __d.Access = (global::Sample.Access)3;
[MapConstant(nameof(Destination.ItemType), typeof(Item))]             // __d.ItemType = typeof(global::Sample.Item);
[MapConstant(nameof(Destination.Codes), new[] { 1, 2 })]              // __d.Codes = new int[] { 1, 2 };
[MapConstant(nameof(Destination.Ratio), 0.1)]                         // __d.Ratio = 0.1d;
[MapConstant(nameof(Destination.Flag), (short)-1)]                    // __d.Flag = (short)-1;
[MapConstant(nameof(Destination.Note), "tab\there")]                  // __d.Note = "tab\there";
```

値はコンパイラーの変換規則でターゲットの型に変換できる必要があります（`1` を `long` や `byte` へ、`null` を参照型へ、など）。`int` への `"abc"`、`float` への `1.5`、`byte` への `short`、数値や別の enum への enum、null を許さない参照型への `null`（や `null` を含む配列）は診断されます（SMP0218）。ファイルローカル型のように生成コードから参照できない値も診断されます（SMP0220）。

`[MapConstant]`・`[MapExpression]`・`[MapUsing]` のターゲットは、マッパーから代入できるプロパティやフィールドです（`"Child.Value"` のようなドット付きパスも使え、途中のメンバーは `[MapProperty]` のパスと同じように扱います。ネストプロパティマッピングを参照）。見つからないもの（綴りの誤り、メソッド、static メンバー）や代入できないもの（`readonly` フィールド）は診断されます（SMP0214）。`[MapUsing]` のメソッドは、ドット付きパスやフィールドのターゲットでも、その型で照合されます。

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

コールバックは、source と destination をその型のまま受け取るほか、値渡しの引数なら、変換できる基底クラスやインターフェイスとしても受け取れます。1 つのコールバックを複数のマッパーで使えます（`private static void Audit(IEntity source, IAuditable destination)`）。構造体の source や destination は、その型そのものだけに渡します。ボックス化すると写しに書き込むことになり、destination に反映されないためです。カスタムパラメーターは、その後ろにその型のまま受け取ります。オーバーロードは C# が呼び出しを結び付けるものを使い、あいまいな呼び出しは診断されます（SMP0102 / SMP0103）。

### 条件付きマッピング（`[MapCondition]`）

条件メソッドは source の値（とカスタムパラメーター）を受け取り、`true` を返したときだけ destination プロパティに代入されます。

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

条件が守るのは、ターゲットのプロパティマッピング（自動マッピング、または `[MapProperty]`。ドット付きパスも可）です。プロパティマッピングのないターゲット（何もマッピングしないもの、`[MapIgnore]` で除外したもの、`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` が代入するもの）では何もしないため、診断されます（SMP0221）。

引数が null を受け付けない条件メソッド（null 許容でない注釈の参照型、または `[DisallowNull]`）には null の source を渡さず、条件を満たさないものとして扱います（`if (source.Name is not null && IsShort(source.Name))`）。null 許容の構造体の source を、中の構造体や、中の値から暗黙に変換できる型で受け取る条件メソッドには `Value` を渡し、source が null なら条件を満たさないものとして扱います（`if (source.Count is not null && IsPositive(source.Count.Value))`）。

### 自動マッピング無効化（`AutoMap = false`）

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]
public static partial void Map(Source source, Destination destination);
// Id のみマッピング。他のプロパティは無視。
```

### Strict モード（`Strict = true`）

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

どのマッピングも代入しない destination のプロパティを警告します（SMP0501）。対象は、setter で（戻り値のあるマッパーではオブジェクト初期化子で設定する `init` アクセサーでも）マッパーが代入できるプロパティのうち、自動マッピングでも属性でも写さず、`[MapIgnore]` で除外もしていないものです。`[MapProperty("Child.Value", ...)]` の `Child` のように、ドット付きのターゲットのパスが中へ写すメンバーは、そのパスで写したものとして扱います。戻り値のあるマッパーでは、コンストラクタでしか設定できないプロパティ（get だけ、またはマッパーのクラスから setter を呼べないもの）も、マッパーが呼べるコンストラクタの引数が受け取るのに、選んだ構築がその引数に値を渡さないときは対象です（[コンストラクタの選び方](#コンストラクタの選び方)）。ソースにあるかどうかは問いません。どのコンストラクタも受け取らないプロパティ（計算で求めるものなど）は対象外で、構築しない void マッパーでは、コンストラクタでしか設定できないプロパティと `init` 専用のプロパティは対象外です。省いた省略可能な引数が設定するはずのプロパティも、コンストラクタが既定値を与えますが対象です。`[MapIgnore]` を付ければ、既定値に任せたまま警告を消せます。`[Obsolete]` のプロパティは対象外です（[廃止されたメンバー](#廃止されたメンバーobsolete)）。

null を受け付けないターゲットに null になりうる値を入れ、ターゲットがそれに `null` か `default` を受け取るマッピングも警告します（SMP0502）。`NullValue`・`NullBehavior.Skip`・`[MapCondition]` でターゲットが受け取るものを指定したマッピングは対象外です。null になりうる値は、宣言からそうと分かるものです。null 許容の型のソースのメンバー、式として値を作るところ（コンストラクタの引数やオブジェクト初期化子の項目。文ならそのときターゲットをそのまま残します）で null 許容の型のメンバーを通して読む値、`[MapFrom]` や `[MapUsing]` のメソッド、変換器、`[MapNested]` / `[MapCollection]` のマッパーが返し、生成コードが `!` を付けて受け取る null 許容の参照（null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるメソッドは除きます。生成したマッパーはこれを宣言します）、null を受け付けないマッパーを呼ばない、null 許容の型の `[MapNested]` / `[MapCollection]` のソースや要素がこれに当たります。null を受け付けないターゲットは、構造体か、`[AllowNull]` なしで null 非許容と注釈した参照（または `[DisallowNull]` 付きのもの）です。null 許容の注釈なし（null 許容コンテキストが無効）で宣言した参照は、null の検査では null 許容として扱いますが（[Null 処理](#null-処理)）、ここでは対象外です。null かどうかを何も言っていないだけで、注釈なしで書いたモデルではすべてのメンバーが警告になるためです。`Dst Map(Src? source)` のように、null 許容で宣言した source を受け取り、null を受け付けない型を返すマッパーは、source が null のとき `default` を返すため、メソッドの位置でターゲット `(return)` として警告します。

enum を別の enum へ写すマッピングは、メンバーを名前で対応させます。ターゲットの enum に同じ名前のメンバーがないメンバーがソースの enum にあると警告します（SMP0503。そのメンバーを並べます）。その値はターゲットで `default` になり、null 許容の enum では `null` になります。メンバーを組み合わせた `[Flags]` の enum の値は来るまで分からないため、メンバーだけを見ます。マッピングに変換器を指定したものは、変換器が変換を引き受けるため対象外です。

SMP0501 は、destination の構築についてのエラーと同じくマッパーのメソッドの位置で、SMP0502 と SMP0503 はマッピングの属性の位置（自動マッピングならメソッドの位置）で報告します（診断ごとの報告の位置は[診断メッセージ](#診断メッセージ)）。メソッドの位置で報告する警告はメソッドの最初の属性の位置から始まるため、`#pragma warning disable` はメソッドの属性より前に置いたときだけ効き、属性の位置で報告する警告はその属性より前に置いたときだけ効きます。属性とメソッドの間に置いても効きません。メソッドに付けた `[SuppressMessage]` はどちらにも効きます：

```csharp
#pragma warning disable SMP0501
[Mapper(Strict = true)]
public static partial Destination Map(Source source);       // 抑えられる
#pragma warning restore SMP0501

[Mapper(Strict = true)]
#pragma warning disable SMP0501
public static partial Destination MapOther(Source source);  // 抑えられない（警告は [Mapper] の位置から始まる）
#pragma warning restore SMP0501

[SuppressMessage("Usage", "SMP0501")]
[Mapper(Strict = true)]
public static partial Destination MapThird(Source source);  // 抑えられる
```

### 廃止されたメンバー（`[Obsolete]`）

自動マッピングは、`[Obsolete]` のプロパティを、警告扱いでもエラー扱いでも、ソースとしても destination としても写しません。プロパティそのもの、読み書きに使うアクセサー、override の元のプロパティのどれに付いていても同じです（C# は override の元のものを報告します）。Strict モードでも知らせませんが、`required` のものはマッピングが必要です（SMP0303）。

属性で名前を書いて指したメンバーは、警告扱いなら使い（C# が CS0618 を報告します）、エラー扱いなら生成コードが使えない（CS0619）ため、その属性の診断で報告します。対象は、プロパティとフィールド、ドット付きパスの各段、`[MapFrom]` のメソッドです（ソースなら SMP0213・SMP0204・SMP0206、ターゲットなら SMP0214）。属性が指すメソッドも、エラー扱いなら一致しないものとして扱います。`Converter`（SMP0104）、`[MapCondition]` のメソッド（SMP0106）、`[MapUsing]` のメソッド（SMP0201）、`[BeforeMap]` / `[AfterMap]` のコールバック（SMP0102 / SMP0103）、`[MapNested]` / `[MapCollection]` のマッパーメソッド（SMP0211 / SMP0210）です。

エラー扱いのコンストラクタは呼ばず、警告扱いのものはほかに方法がないときだけ呼びます（[コンストラクタの選び方](#コンストラクタの選び方)）。ドット付きのターゲットの途中のメンバーは、`new T()` が結び付くコンストラクタを呼べるときに作ります。それがエラー扱いなら、その型は作れないものとして扱い、destination が持つメンバーに書き込みます。警告扱いなら、ほかに作る方法がないため呼びます（警告が出ます）。

生成コードが自動の変換で呼ぶメンバーも、エラー扱いの `[Obsolete]` なら呼びません。対象は、変換演算子（`implicit` / `explicit`）、`[ValueConverter]` / `[CollectionConverter]` のクラスのメソッド、変換で使う `ToString(format, provider)` と `Parse`、コレクションのクラスを作るコンストラクタです。ほかの変換があればそれを使い（専用のメソッドの代わりに変換クラスの汎用のメソッド、span を受け取る `Parse` の代わりに `string` を受け取るもの）、なければ、変換がない（SMP0402）、変換メソッドが一致しない（SMP0104）、生成コードが作れないコレクション（SMP0217）として報告します。警告扱いなら呼びます（CS0618 が出ます）。

`[Obsolete]` の enum メンバーは、警告扱いでもエラー扱いでも、enum から別の enum（メンバーは従来どおり名前で対応付けます）や文字列への変換、文字列からの変換の switch と、`[MapConstant]` や `NullValue` の enum の定数（同じ値の廃止されていないメンバーがあればその名前で書きます）では、生成コードが警告もエラーも出さないよう、`(Color)2` のように値のキャストで書きます。

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

`Location.Lat` の `GeoPoint? Location` のように、パスの途中の null 許容の構造体は、同じ検査の下で中の構造体を通して読みます（`source.Location.Value.Lat`）。パスの末端の値はメンバーと同じく変換します。enum は別の enum や文字列との間ではメンバーの名前で、数値との間ではキャストで変換し、メソッドのカルチャと書式も当てはまります。

途中のメンバーが null のときは、`NullValue` を指定したマッピングはその値を入れ、ほかはターゲットをそのまま残します。`NullBehavior.Skip` と、調べる source の値がない `[MapCondition]` 付きのマッピングも残します：

```csharp
// [MapProperty("ChildName", "Child.Name", NullValue = "none")]
if (source.Child is not null)
{
    destination.ChildId   = source.Child.Id;
    destination.ChildName = source.Child.Name ?? "none";
}
else
{
    destination.ChildName = "none";
}
```

### Unflatten（フラット source → ネスト destination）

```csharp
[Mapper]
[MapProperty("Child1.Value", "Value1")]
[MapProperty("Child2.Value", "Value2")]
public static partial void Map(Source source, Destination destination);
```

destination 側の中間オブジェクトは、そこを通して値を代入するときに自動インスタンス化されます：

```csharp
destination.Child1 ??= new DestinationChild();
destination.Child2 ??= new DestinationChild();
destination.Child1.Value = source.Value1;
destination.Child2.Value = source.Value2;
```

ターゲットをそのまま残すことがある、それ自身の条件の下で行う代入は、その条件の中で、代入の直前に中間オブジェクトを作ります。`NullBehavior.Skip`、`[MapCondition]`、ソースのパスの途中の null チェック、source が null のときに呼ばない `Converter` の場合です。そこを通して何も代入しなければ、中間のメンバーは null のままです。渡されなかったメンバーをそのまま残す更新などが、この形です。条件のない代入も同じ中間を通るときは、今までどおり先に作ります。ソースのパスの途中が null のときに `NullValue` を入れる代入は、どちらの場合にも作ります。戻り値のあるマッパーも、オブジェクト初期化子の中を除いて同じように作ります：

```csharp
// [MapProperty("Address.City", nameof(Request.City), NullBehavior = NullBehavior.Skip)]
if (request.City is not null)
{
    customer.Address ??= new Address();
    customer.Address.City = request.City!;
}
```

マッパーから代入できない中間のメンバー（get だけ、`init` 専用、マッパーから呼べないセッター）や、マッパーが作れない型（抽象クラス、インターフェース、引数なしで呼べるコンストラクタがない型、required メンバーのある型）のメンバーはインスタンス化せず、持っているインスタンスを埋めます。null のときは何も代入しません：

```csharp
// Destination.Child: public DestinationChild Child { get; } = new();
if (destination.Child is not null)
{
    destination.Child.Value = source.Value1;
}
```

struct のプロパティは値なので、ローカルに写して埋め、書き戻します。struct のフィールドはそのまま埋めます。書き戻せないもの（get だけのプロパティ、`readonly` フィールド）は診断されます（SMP0214）：

```csharp
// Destination.Point: public Point Point { get; set; } (a struct)
{
    var __copy0 = destination.Point;
    __copy0.X = source.Value1;
    destination.Point = __copy0;
}
```

パスの末尾の `init` 専用メンバーは、オブジェクト初期化子でしか設定できません。戻り値のあるマッパーは、通る途中のメンバーを作りながら初期化子で設定します。途中のメンバーは、初期化子で代入でき、作れる必要があります。void マッパーでは設定できず（SMP0302）、初期化子でも作れないパス（get だけのメンバーを通るものなど）は診断されます（SMP0214）：

```csharp
// DestinationChild.Value: public int Value { get; init; }
var __d = new Destination()
{
    Child = new DestinationChild()
    {
        Value = source.Value1,
    },
};
```

`[MapConstant]`・`[MapExpression]`・`[MapUsing]` のドット付きのターゲットも、途中のメンバーを同じように扱います。

メンバーの中へのドット付きパスは、`[MapProperty]`・`[MapConstant]`・`[MapExpression]`・`[MapUsing]` のどれでも、そのメンバーの自動マッピングに代わり、メンバー全体は自動ではマッピングされません。パスは destination が持つメンバーか作ったメンバーに書き込み、ソースのオブジェクトには書き込みません。`[MapIgnore]` で除外したメンバーでも、その中へのドット付きパスはマッピングされます。属性でメンバー全体をマッピングしたうえで、その中へドット付きパスを書くことはできません（SMP0101）。戻り値のあるマッパーが呼ぶコンストラクタが引数から代入するメンバー（位置指定 `record` の引数など）の中へも書けません。パスがコンストラクタに渡したオブジェクトに書き込むことになるためです（SMP0222）。戻り値のあるマッパーが設定する `required` メンバーは、型を作れればオブジェクト初期化子で作り、パスは構築後にその中へ書き込みます（末尾が `init` 専用のメンバーなら初期化子の中で書きます）。

---

## コレクションマッピング（`[MapCollection]`）

要素マッパーメソッドの明示的な指定が必要です（ないときは SMP0210。メッセージで指定がないことを示します）。ソースはソースの型のプロパティで、ドット付きのパスにはできません（SMP0206）。

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

ループはソースとターゲットのコレクション型に合わせてインラインで生成されます。ソースコレクションが null の場合はターゲットに `default` を代入します。ターゲットには、ループが作るコレクション（`List<T>` とそのインターフェースには `List<T>`、配列、集合には `HashSet<T>`、`IDictionary<TKey, TValue>` と `IReadOnlyDictionary<TKey, TValue>` には `Dictionary<TKey, TValue>`（要素マッパーは `KeyValuePair<TKey, TValue>` の組を写します）、イミュータブル・フローズンなコレクションにはその型）を代入します。イミュータブル・フローズンなコレクションで作れるのは `ImmutableArray<T>`・`ImmutableList<T>`・`ImmutableHashSet<T>` とそれらのインターフェース、`FrozenSet<T>` で、`ImmutableDictionary<TKey, TValue>` や `FrozenDictionary<TKey, TValue>` などほかのものは、コレクション変換器で作る場合を除き診断されます（SMP0217）。`ObservableCollection<T>` や `class ItemList : List<Item>` のような、マッパーが作れるコレクションクラスは、その型のコンストラクタで作って `ICollection<T>` として詰めます。作れないものは、コレクション変換器で作る場合を除き診断されます（SMP0217）。void の要素マッパー `(SourceChild, DestinationChild)` は `new DestinationChild()` で作ったインスタンスを埋めるため、要素の型は `new()` で作れる必要があります（そうでなければ SMP0210）。作るコレクションはターゲットの要素の null 許容注釈を保ち（`List<DestinationChild?>`）、`DestinationChild? MapChild(SourceChild? source)` のように null を受け取るマッパー（null を返すのは source が null のときだけです）の結果は、null 許容でない要素には `[MapNested]` のターゲットと同じく `!` を付けて受け取ります。ただし、要素が null でなく、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるとき（生成したマッパーはこれを宣言します）は、そのまま受け取ります（`List<SourceChild>` のソースなら `__dst[__i] = MapChild(__src[__i]);`）。要素マッパーは `[MapNested]` のマッパーと同じ規則で型を照合します。null 許容の構造体の要素は、構造体を受け取るマッパーに中の値として渡し、null の要素は `default` になります。null になりうる参照の要素（null 許容のもの、または null 許容の注釈なしで宣言したもの）も、引数が null を受け付けないマッパーには値があるときだけ同じように渡し、null の要素は `default` になります（`__src[__i] is { } __value ? MapChild(__value) : default!`）。マッパーをデリゲートとして受け取るコレクション変換器には、すべての要素が渡ります。

独自のコレクションクラスは、基底の型やインターフェースで実装している `IEnumerable<T>` によって、ソースでもターゲットでもコレクションとして扱います。

```csharp
public class SourceChildList : List<SourceChild> { }
public class DestinationChildList : List<DestinationChild> { }

// Source.Children: SourceChildList / Destination.Children: DestinationChildList
// 生成コード:
{
    var __srcColl = source.Children;
    var __coll = new DestinationChildList();
    var __items = (ICollection<DestinationChild>)__coll;
    foreach (var __item in __srcColl)
    {
        __items.Add(MapChild(__item));
    }
    destination.Children = __coll;
}
```

マッパーが写すのはオブジェクトで、コレクションではありません。source や destination が、フレームワークのコレクション（リスト・集合・辞書とそれらのインターフェース、イミュータブル・フローズン・コンカレント・ObjectModel のもの）、それを継承したクラス（`class ItemList : List<Item>`）、配列、タプルのマッパーは診断されます（SMP0007）。コレクションのメンバー（`Count`・`Capacity`）を写すだけで、要素をひとつも写さないためです。要素は要素の型のマッパーで写すか、コレクションを持つ型を `[MapCollection]` で写してください：

```csharp
[Mapper]
public static partial ItemDto ToDto(Item source);

// [Mapper] List<ItemDto> ToDtos(List<Item> source) は診断される
var dtos = items.Select(ToDto).ToList();
```

`IEnumerable<T>` を実装するだけの独自の型（件数を持つページなど）は、ほかの型と同じくメンバーで写します。

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

ターゲットは `ICollection<T>` として空にして詰め直すため、宣言の型はそれを実装し、設計上読み取り専用でないものである必要があります。`IReadOnlyList<T>`・`IReadOnlyCollection<T>`・`IEnumerable<T>`・配列・イミュータブルやフローズンなコレクション・`ReadOnlyCollection<T>` は診断されます（SMP0219）。ターゲットが null のときは次のとおりです。

- マッパーから代入できるプロパティには、その型の新しいインスタンス（例: `new ObservableCollection<T>()`）を作ります。インターフェースには `List<T>`（`ISet<T>` には `HashSet<T>`、`IDictionary<TKey, TValue>` には `Dictionary<TKey, TValue>`）を作ります。どちらにも当たらない型は診断されます（SMP0217）。
- 代入できないプロパティ（get だけ、`private` や `init` のセッター）は null のまま残します。インスタンスを持っていれば、それを詰め直します。

```csharp
// Destination.Children: public List<DestinationChild> Children { get; } = [];
{
    if (destination.Children is not null)
    {
        destination.Children.Clear();
        ...
    }
}
```

`IList<T>` などで宣言したターゲットに、実行時に読み取り専用のインスタンス（配列など）が入っていると、`Clear` で `NotSupportedException` になります。`InPlace` の約束どおりインスタンスはそのまま保ち、呼び出し側の知らないうちに置き換えることはしません。`InPlace` は常にループを生成し、コレクション変換器は使いません。元のコレクションが null のときは、ターゲットを空にも置き換えもせず、そのまま残します。戻り値のあるマッパーが作る destination の `required` メンバーは構築の前に設定する必要があり、詰め直すインスタンスがないため、呼ぶコンストラクタに `[SetsRequiredMembers]` がない限り診断されます（SMP0219）。`init` 専用のメンバーは、get だけのものと同じく、持っているインスタンスを詰め直します。

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

`DestinationChild MapChild(SourceChild? source)` のように引数が null を受け付けるマッパーには、null の source のメンバーも渡し、ターゲットが受け取るものはマッパーが決めます（`destination.Child = MapChild(source.Child);`）。void のマッパーは、ターゲットのために作ったインスタンスを埋めます。引数が null を受け付けないマッパーは値があるときだけ呼び、source が null のときは上のとおりターゲットに `default` を入れます。null 許容の注釈なしで宣言した source のメンバーも null になりえます（[Null 処理](#null-処理)）。

null 許容の参照を返すマッパーの結果は、null 許容でないターゲットには `!` を付けて入れます。ただし、マッパーに null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるとき（生成したマッパーはこれを宣言します）はそのまま入れます。null 許容でない source と `DestinationChild? MapChild(SourceChild? source)` なら `destination.Child = MapChild(source.Child);` です。void のマッパーが埋めるインスタンスは、ターゲットの型引数の null 許容注釈を保って作ります（`new Box<string?>()`）。

マッパーは、source のメンバーをその型のまま、または暗黙の参照変換で変換できる型（基底クラスやインターフェース。値渡しのとき）として受け取り、ターゲットの型、同じように変換できる型（実装するインターフェースの型のターゲットに対するクラスなど）、または null 許容の構造体のターゲットに対するその構造体を返します。null 許容の構造体のメンバーは、構造体を受け取るマッパーに、null を調べた後で中の値として渡し、null のときは参照型が null のときと同じく `default` を入れます（`source.Point is not null ? MapPoint(source.Point.Value) : default!`）。void のマッパーは、ターゲットのために作ったインスタンスを、その型のまま、または値渡しなら変換できる型として受け取ります。このように変換できないマッパーは一致しません（SMP0211）。オーバーロードは、C# が呼び出しを結び付けるものを使います。一致しないメソッド（別の型を返す、より具体的なもの、ジェネリックメソッド、省略可能な引数のあるもの、エラー扱いの `[Obsolete]` のもの）に結び付く呼び出しは診断されます（SMP0211。`[MapCollection]` の要素のマッパーは SMP0210）。

`init` 専用のターゲットや、呼ぶコンストラクタが設定しない `required` のターゲットには、戻り値のあるマッパーが `[MapCollection]` と同じく構築の前に値を作り、オブジェクト初期化子で入れます（[record / プライマリコンストラクタ対応](#record--プライマリコンストラクタ対応)を参照）。

---

## record / プライマリコンストラクタ対応

destination 型が `record` またはプライマリコンストラクタを持つクラスの場合、ジェネレーターが自動的にコンストラクタ呼び出しを生成します。

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

void マッパーは構築を行わないため、destination のコンストラクタは影響しません。コンストラクタでしか代入されないメンバーは、get だけのプロパティと同じく自動マッピングの対象から外れます。

> `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティなど）にも、`[MapProperty]` で指定したコンストラクタでしか代入されないメンバーにも代入できません（SMP0302）。

コンストラクタが代入するメンバーの中へのドット付きのターゲット（`record Dst(Child Child)` への `[MapProperty("Child.Value", ...)]` など）は診断されます（SMP0222）。コンストラクタに渡したオブジェクトに書き込むことになり、引数がソースのものをそのまま渡すときはソースのオブジェクトを書き換えるためです。

コンストラクタが代入するメンバーには、`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` の値も使えます。値は引数に渡します（`new Dst(Build(src))`）。`[MapNested]` と `[MapCollection]` は構築の前に値を作ります。`InPlace` は構築の前に詰め直すインスタンスがないため使えません（SMP0219）。

`init` 専用のメンバーと、呼ぶコンストラクタが設定しない `required` のメンバーにも、同じく構築の前に値を作ってオブジェクト初期化子で入れます。null の扱い、要素の注釈、マッパーの照合はほかのターゲットと同じです。void マッパーは `init` 専用のメンバーに代入できません（SMP0212）。

```csharp
// Destination.Children: public required IReadOnlyList<DestinationChild> Children { get; init; }
IReadOnlyList<DestinationChild> __init0;
{
    ...
    __init0 = __list;
}
var __d = new Destination()
{
    Children = __init0,
};
```

destination の `required` メンバー（プロパティとフィールド。アクセシビリティを問わず、基底クラスのものも含む）は、戻り値のあるマッパーがオブジェクト初期化子で設定するため、どれもマッピングが必要で（SMP0303）、除外できません（SMP0216）。自動マッピングと `[MapProperty]` は public のプロパティを対象にし、`[MapConstant]`・`[MapExpression]`・`[MapUsing]` は `internal` のメンバーにも使えます。ドット付きパスが中へ書き込む `required` メンバーは、型を作れればオブジェクト初期化子で作ります。呼ぶコンストラクタが引数から代入する `required` メンバーは診断されます（SMP0304）。オブジェクト初期化子で設定し直すことになり、コンストラクタが引数から作ったものを上書きするため、コンストラクタに `[SetsRequiredMembers]` が必要です。呼ぶコンストラクタに `[SetsRequiredMembers]` があれば必須ではなく、マッピングしないものはコンストラクタが設定した値のまま、マッピングするものは今までどおりオブジェクト初期化子で設定し、`[MapNested]` / `[MapCollection]` も構築後に代入できます。void マッパーはすでにあるインスタンスを埋めるので、関係しません。

### コンストラクタの選び方

戻り値のあるマッパーが呼ぶコンストラクタは、次の規則で選びます。

- 候補は、引数を持つと宣言されたコンストラクタのうち、マッパーのクラスから呼べて、どの引数も値で受け取れるものです。呼べない `private` や `protected` のもの、エラーの `[Obsolete]` のもの、`ref`・`out`・`ref readonly` の引数を持つものは候補にしません（`in` の引数は値で受け取れます）。abstract のクラスには候補がありません。
- コンストラクタでしか受け取れない宛先（同じ名前のメンバーがない引数名や、マッパーのクラスから setter を呼べないメンバー）に属性が値を与えるときは、それでコンストラクタを選びます。マッピングがどの引数にも値を持つ候補のうち、その宛先を最も多く受け取るもの、次に警告扱いの `[Obsolete]` でないもの、次に最長のもの、次に先に宣言したものを呼びます。宛先を受け取れるのが警告扱いの `[Obsolete]` のコンストラクタだけなら、それを呼びます（CS0618 の警告が出ます）。setter で受け取れる宛先はコンストラクタを選ぶ理由にしないため、setter をすべて呼べる型の構築は変わりません。そのような候補のどれも受け取れない宛先は診断されます（SMP0214）。
- それ以外では、警告扱いの `[Obsolete]`（CS0618）のコンストラクタは、ほかに destination を作る方法がないときだけ呼びます。マッピングがどの引数にも値を持つほかの候補か、引数なしで呼べるコンストラクタがあれば選ばず、警告扱いの `[Obsolete]` の引数なしのコンストラクタも、引数なしで作る方法として数えません。
- それ以外は、構築に引数を使うかを最長の候補で決めます。型が `record` である、構築後にマッパーが代入できる対応プロパティを持たない引数がある（プロパティがない、get だけ、`init` 専用、`private set` のようにマッパーのクラスから setter を呼べない）、または public なパラメータレスコンストラクタがない場合に使います。それ以外は `new Dst()` + プロパティ代入（init 専用メンバーはオブジェクト初期化子で代入）を生成します。
- 呼ぶのは、マッピングがどの引数にも値を持つ候補のうち最長のものです。値は、引数か、引数が代入するメンバーに対応するソースのプロパティ、またはどちらかを指す属性です。`[MapIgnore]` がメンバー（または引数そのもの）を指す引数には値がありません。値のない省略可能な引数（既定値、`[Optional]`、`params`）は渡さずに既定値に任せ、その後ろの引数は名前付きで渡します（`new Dst(src.A, c: src.C)`）。省いた呼び出しをほかのコンストラクタも受け取れる（同じ型で、残りの引数が省略可能）候補は、呼び出しがそちらに結び付くか、あいまいになるため選びません。その候補も引数を必要としない（どの引数もマッパーが代入できるプロパティに対応し、public なパラメータレスコンストラクタがある）ときは、`new Dst()` を生成します。
- どの候補にも値がそろわないときは、引数なしで作れる destination なら `new Dst()` で作り、コンストラクタでしか代入されないメンバーは写しません。引数なしで作れないものは最長の候補を使い、値のない引数を診断します（SMP0301。`[MapIgnore]` が指すものは SMP0216）。
- 作れない destination（abstract のクラス、インターフェイス、候補も引数なしで呼べるコンストラクタもない型、`new()` と `struct` のどちらの制約もない型パラメーター）は診断されます（SMP0305）。

規則は、エラー扱いの `[Obsolete]` は候補にしない、属性の宛先で選ぶ、警告扱いの `[Obsolete]` は避ける、最長、宣言順の順に当てはめます。

```csharp
public class Order
{
    public Order() { }
    public Order(int id, string name) { Id = id; Name = name; }

    public int Id { get; private set; }
    public string Name { get; set; } = "";
}

[Mapper]
public static partial Order Map(OrderSource src);   // new Order(src.Id, src.Name)。OrderSource に Id がなければ new Order()
```

### コンストラクタ引数の変換

コンストラクタ引数は通常のプロパティ代入と同じ変換パイプラインを通るため、型変換・`Converter`・`NullValue`・`Culture` / フォーマット指定がすべて適用されます。オブジェクト初期化子で代入される `init` 専用メンバーも同様です。引数の値は、代入先のメンバーの型ではなく、引数の型に対して確かめて変換します。`int` のプロパティを代入する `string` の引数には、`string` のプロパティと同じく文字列への変換を使います。`Converter`・`NullValue`・属性の値も、引数の型のプロパティと同じ規則で確かめます。メンバーの型を返すコンバーターやメソッドは、引数の型が違えば、その型のプロパティに対するときと同じく診断されます（SMP0105・SMP0202・SMP0205・SMP0211・SMP0218）。

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

`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` も、このような引数を名前で指せます。引数を自分の名前で指す `[MapProperty]` は、別の綴りのメンバーも同じ引数に当たる場合（`Ordinal` の比較での引数 `value` とプロパティ `Value`）でも、その `[MapProperty]` を引数に使います。

---

## Null 処理

| ソース型 | デスティネーション型 | 動作 |
|----------|---------------------|------|
| `T?` | `T?` | そのままコピー（null も含む） |
| `T?` | `T`（末端） | null の場合 `default!` を代入 |
| `T` | `T?` | そのままコピー |
| `T` | `T` | そのままコピー |

**source 側**の nullable 中間パスには、`[MapProperty]` でも `[MapFrom]` でも `if (... is not null)` ガードが付き、null 許容の構造体は中の構造体を通して読みます（`source.Location.Value.Lat`）。途中が null のときは、`NullValue` を指定したマッピングはその値を入れ、ほかはターゲットをそのまま残します。
**destination 側**の nullable 中間パスは、マッパーから代入でき、作れれば `??= new` で自動インスタンス化され、そうでなければ持っているインスタンスを埋めます。

null 許容の注釈なし（`#nullable disable` や、注釈なしでビルドしたライブラリ）で宣言した参照型は null かどうかを何も言っていないため、その値は null になりえます。そのような型の元の引数、source のメンバー、source のコレクションの要素は、null 許容のものと同じく扱います。読み進める前や、引数が null を受け付けない変換器・条件・`[MapNested]` のマッパー・要素のマッパーに渡す前に null を調べ、`NullValue` と `NullBehavior.Skip` も当てはまります。Strict モードでは、これらを null になりうる値として警告しません（SMP0502）。

元の引数（void マッパーでは宛先の引数も）を `Map(Src? source)` のように null 許容で宣言すると、写す前に検査します。null 許容の注釈なしで宣言した元の引数と void マッパーの宛先の引数も同じです。カスタムパラメーターはそのまま渡します。null のときは何も写さず、戻り値のあるマッパーは `default` を返し、void マッパーは宛先に触れずに戻ります。`Dst? Map(Src? source)` や `Point? Map(Src? source)` のように、元の引数が null になりうる、null 許容の型を返すマッパーは、元の引数が null のときだけ null を返すため、実装に `[return: NotNullIfNotNull("source")]`（引数の名前で）を付けます。null でない引数を渡した呼び出し側は、null 許容の警告なしで結果を使えます。宣言の側に同じ属性を付けてもかまいません。マッパーのクラスから使える `NotNullIfNotNullAttribute` がコンパイルにないとき（その写しのない .NET Standard 2.0 や .NET Framework）は付けません。

`Point? Map(Src source)` のように null 許容の構造体を戻り値の型にすると、その中の構造体として作って埋めます。`Map(Point? source)` のように元の引数を null 許容の構造体にすると、その中の構造体のメンバーを持たないため診断されます（SMP0006）。構造体そのものを受け取り、null は呼び出す前に調べてください。

生成コードが作るインスタンスは、宣言どおり型引数の null 許容注釈を保ちます。`Box<string?>` と宣言した destination は `new Box<string?>()` で作り、ドット付きのターゲットの途中のメンバー、オブジェクト初期化子で作る `required` のメンバー、`[MapNested]` / `[MapCollection]` の void のマッパーが埋めるインスタンスも同じです。

---

## 型変換

同型・暗黙的変換可能な代入はコンバーター不要で直接生成されます。数値の拡大変換、値からその null 許容型への変換、暗黙の参照変換（変性によるものを含む。`IReadOnlyList<Circle>` から `IReadOnlyList<Shape>`、`Circle[]` から `Shape[]` など）がこれに当たります。両側が同じ enum ならそのまま写すため、フラグの組み合わせのようにメンバーのない値も保ちます。
変換が必要な場合は、以下のように `DefaultValueConverter` のその型のスペシャライズドメソッドを呼びます。ほかの型は、その型の変換演算子、`Parse`（文字列から `IParsable<T>` を実装する型へ）、`ToString(format, provider)`（文字列へ）で変換します。数値をより狭い数値型へ写すときは C# のキャストと同じくキャストし（`(int)source.LongValue`）、enum はメンバーの switch か、数値との間のキャストで変換します。どれでも変換できないものは、`[ValueConverter]` のクラスが引き受けない限り診断されます（SMP0402、後述）。参照型のユーザー定義の変換は null 許容に持ち上げられないため、null になりうる値は値があるときだけ演算子を通します（`implicit operator string(Email email)` のある `Email?` なら `source.Email is not null ? (string)source.Email : null`）。

別の enum へ写すときは、メンバーを名前で対応させます。フラグの組み合わせのように、ターゲットに同じ名前のメンバーがない値は `default` になり、null 許容の enum のターゲットでは `null` になります。文字列から enum へ写すときも同じく名前で対応させ、同じ名前のメンバーがない文字列は `Enum.Parse` に渡します（解析できなければ例外になります）。null 許容の enum のターゲットには `Enum.TryParse` で解析し、解析できなければ `null` にします（数字の文字列はその値になります）。Strict モードでは、ターゲットの enum に同じ名前のメンバーがないソースの enum のメンバーを警告します（SMP0503）。

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

### 既定の書式

`Culture` も書式も指定しないとき、値は次のように文字列との間で変換します（`DefaultValueConverter`、インバリアントカルチャ）：

| 型 | `string` へ | `string` から |
|----|-------------|---------------|
| 数値（`int`・`long`・`double`・`decimal`・`Half`・`Int128`・`BigInteger` など） | `ToString(CultureInfo.InvariantCulture)` | `Parse(text, CultureInfo.InvariantCulture)` |
| `bool` | `True` / `False` | `bool.Parse` |
| `char` | その文字 | `char.Parse` |
| `Guid` | `D`（`00000000-0000-0000-0000-000000000000`） | `Guid.Parse` |
| `DateTime` | ラウンドトリップ書式の `O`（`2024-01-02T03:04:05.6780000Z`。ローカル時刻ならオフセット付き、種類が未指定なら付けない） | `DateTimeStyles.RoundtripKind` 付きの `DateTime.Parse`。文字列の示す種類を保つ（`Z` なら UTC、オフセットがあればローカル、どちらもなければ未指定） |
| `DateTimeOffset` | `O`（`2024-01-02T03:04:05.0000000+09:00`） | `DateTimeOffset.Parse` |
| `DateOnly` | `O`（`2024-01-02`） | `DateOnly.Parse` |
| `TimeOnly` | `O`（`03:04:05.0000000`） | `TimeOnly.Parse` |
| `TimeSpan` | `c`（`1.02:03:04.5000000`） | `TimeSpan.Parse` |
| enum | メンバー名（メンバーのない値は `ToString()` の結果） | メンバー名で対応（なければ `Enum.Parse`、null 許容のターゲットなら `Enum.TryParse`） |

`Culture` を指定するとそのカルチャの書式（`ToString(culture)`・`Parse(text, culture)`）になり、`DateTimeFormat` / `NumberFormat` で書式を指定できます（[Culture / Format](#culture--format) を参照）。

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

`[MapProperty]` の `Converter` は、`[MapUsing]` のメソッドがソースを受け取るのと同じようにソースのメンバーを受け取ります。暗黙に変換できる型、null 許容の構造体なら中の構造体としても受け取れます（静的メソッドによる値計算を参照）。返す型は、ターゲットの型か、`[MapUsing]` のメソッドと同じく暗黙に変換できる型です（そうでなければ SMP0105）。null 許容の参照を返し、ターゲットが null 許容でないときは `!` を付けて受け取ります。null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるときは、そのまま受け取ります。

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

`Culture`・`DateTimeFormat`・`NumberFormat` は、それぞれ独立に決まります。`[MapperProfile(Culture = "ja-JP", NumberFormat = "N0")]` の下では、`[Mapper(NumberFormat = "N2")]` は ja-JP と N2、`[Mapper(Culture = "de-DE")]` は de-DE と N0 で書式化します。

カルチャを指定すると、変換器のスペシャライズドメソッドは、カルチャと書式を受け取るオーバーロードで呼ばれます（例: `DefaultValueConverter.ConvertToString(int source, IFormatProvider culture, string? format)`）。独自の `[ValueConverter]` は、使うスペシャライズドメソッドごとにこのオーバーロードを用意する必要があります（そうでなければ SMP0104）。

解決された `CultureInfo` は生成クラス内で `static readonly` フィールドとしてキャッシュされ、変換ごとの `GetCultureInfo(...)` 呼び出しコストを排除します。

> `Culture` なしで `DateTimeFormat` / `NumberFormat` のみ指定するとコンパイルエラー（SMP0401）。

> `[Mapper]` と `[MapperProfile]` の `DateTimeFormat` は、マッパーが変換する日付と時刻の型すべて（`DateTime`・`DateTimeOffset`・`DateOnly`・`TimeOnly`・`TimeSpan`）に使われます。型ごとに書式を分けるときは、`[MapProperty]` の `DateTimeFormat` で指定してください。`TimeSpan` の書式はほかの型と書き方が違い（`hh\:mm` のように区切りをエスケープします）、日付向けの書式は `TimeSpan` や `TimeOnly` では実行時に失敗します（`FormatException`）。

> `Culture` はカルチャ名である必要があります。英数字のサブタグをハイフンでつないだもの（`ja-JP`、`zh-Hant-TW` など）で、アンダースコアの後に並べ替え順を付けられます（`de-DE_phoneb` など）。そうでなければ診断されます（SMP0404）。

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

原因が属性の診断は、その属性の位置で報告します。互いに食い違う 2 つの属性（SMP0101）では 2 つ目の属性、クラスの `[MapperProfile]` や `[ValueConverter]` が与えた値が原因なら、その属性の位置です。メソッドそのもの、自動マッピング、destination の構築（SMP0301・SMP0303・SMP0304・SMP0305）、Strict モードの未マップのプロパティ（SMP0501）についての診断はマッパーのメソッドの位置で、Strict モードのほかの警告（SMP0502・SMP0503）はマッピングの属性の位置（自動マッピングならメソッドの位置）で報告します。メソッドの位置で報告する警告はメソッドの最初の属性の位置から始まるため、`#pragma warning disable` は属性より前に置いたときだけ効きます（[Strict モード](#strict-モードstrict--true)）。

エラーを報告したマッパーには、実装がない代わりに `NotImplementedException` を投げる実装を生成し、エラーに実装がないこと（CS8795）が並ばないようにします。ビルドはエラーで止まるため、この実装が動くことはありません。`static partial` でないメソッドや、`partial` でない型・`file` の型の中のメソッド（SMP0001）は、生成コードが実装できないため生成しません。アクセシビリティ修飾子なしで宣言したメソッドには、宣言と同じく修飾子なしで生成します。

| コード | 説明 | 重大度 |
|--------|------|--------|
| SMP0001 | マッパーメソッドは `static partial` で、含む型もすべて `partial` で `file` の型でない必要がある | エラー |
| SMP0002 | マッパーメソッドに引数がない、または `void` なのに source の後に宛先の引数がない | エラー |
| SMP0003 | カスタムパラメーターの型が重複している | エラー |
| SMP0004 | マッパーメソッドのパラメーター名が `__` で始まっている（生成コードの予約名） | エラー |
| SMP0005 | 生成コードが扱えない修飾子がパラメーターに付いている（`out`、void マッパーの struct の宛先の修飾子なし・`in`・`ref readonly`。struct の宛先は `ref` で受け取る） | エラー |
| SMP0006 | 元の引数が null 許容の値型で、中の構造体のメンバーを持たない | エラー |
| SMP0007 | source や destination がコレクション・配列・タプルで、マッパーは丸ごとは写さない（要素の型のマッパーで要素を写すか、コレクションを持つ型を `[MapCollection]` で写す） | エラー |
| SMP0008 | マッパーメソッドが参照（`ref` / `ref readonly`）で返し、作った destination を返せない | エラー |
| SMP0101 | 同一目的プロパティへのマッピングが重複している、メンバーとその中（`Child` と `Child.Value`）を両方マッピングしている、または同じターゲットに `[MapIgnore]` とマッピング属性を指定している | エラー |
| SMP0102 | `BeforeMap` メソッドのシグネチャが一致しない | エラー |
| SMP0103 | `AfterMap` メソッドのシグネチャが一致しない | エラー |
| SMP0104 | コンバーターメソッドが見つからない、またはシグネチャが一致しない | エラー |
| SMP0105 | コンバーターの戻り値型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0106 | プロパティ条件メソッドのシグネチャが一致しない | エラー |
| SMP0201 | `MapUsing` メソッドのシグネチャが一致しない | エラー |
| SMP0202 | `MapUsing` メソッドの戻り値型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0203 | `[MapFrom]` ターゲットプロパティが目的型に存在しない | エラー |
| SMP0204 | `MapFrom` メンバーは、マッパーから呼べるソース型の引数なしメソッドまたはプロパティパス（継承したものを含む）である必要がある | エラー |
| SMP0205 | `MapFrom` メンバーの型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0206 | `[MapCollection]` / `[MapNested]` のソースプロパティが見つからない（ソースはソースの型のプロパティで、ドット付きのパスにはできない） | エラー |
| SMP0207 | `[MapCollection]` / `[MapNested]` のターゲットプロパティが見つからない | エラー |
| SMP0208 | `[MapCollection]` のソースプロパティがコレクション型ではない | エラー |
| SMP0209 | `[MapCollection]` のターゲットプロパティがコレクション型ではない | エラー |
| SMP0210 | `MapCollection` 要素マッパーメソッドが見つからないまたはシグネチャが一致しない、または `Mapper` を指定していない | エラー |
| SMP0211 | `MapNested` マッパーメソッドが見つからないまたはシグネチャが一致しない、または `Mapper` を指定していない | エラー |
| SMP0212 | `[MapCollection]` / `[MapNested]` の対象に代入できない（マッパーから呼べるセッターも `init` アクセサーもない、または void マッパーで init 専用。`InPlace` は持っているインスタンスを詰め直す） | エラー |
| SMP0213 | `[MapProperty]` のソースプロパティが見つからない | エラー |
| SMP0214 | マッピングのターゲットが見つからない、または代入できない（マッパーから呼べるセッターがない、`readonly` フィールド、生成コードが通れないドット付きパス）。`[MapIgnore]` / `[MapCondition]` のターゲットも含む | エラー |
| SMP0215 | コンストラクタ / 初期化子経由で代入されるターゲットに `[MapCondition]` / `NullBehavior.Skip` を指定 | エラー |
| SMP0216 | destination をほかに作る方法がないコンストラクタの引数が代入するメンバーや、戻り値のあるマッパーが作る destination の `required` メンバーに `[MapIgnore]` を指定 | エラー |
| SMP0217 | `[MapCollection]` の対象が、生成コードの作るコレクションを受け取れない | エラー |
| SMP0218 | `[MapConstant]` の値や `NullValue` をターゲットの型に代入できない、または null を受け取らないターゲットに null を入れる | エラー |
| SMP0219 | `InPlace` の対象を空にして詰め直せない（`ICollection<T>` を実装しない、設計上読み取り専用、または構築前にインスタンスがない、コンストラクタが受け取るメンバーや `required` のメンバー） | エラー |
| SMP0220 | `[MapConstant]` の値や `NullValue` を生成コードに書けない（ファイルローカル型など） | エラー |
| SMP0221 | `[MapCondition]` のターゲットに、条件が守るプロパティマッピングがない | エラー |
| SMP0222 | 戻り値のあるマッパーが呼ぶコンストラクタが引数から代入するメンバーの中へ、ドット付きのターゲットを書いている | エラー |
| SMP0223 | `[MapIgnore]` のターゲットがドット付きパス（メンバーの中のメンバー）で、自動マッピングが単独では代入しない | エラー |
| SMP0301 | destination をほかに作る方法がないコンストラクタの引数に値がない（一致するソースプロパティも属性もなく、省略可能でもない） | エラー |
| SMP0302 | `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティ、ドット付きパスの末尾のものなど）やコンストラクタでしか代入されないメンバーに代入できない | エラー |
| SMP0303 | 戻り値のあるマッパーが作る destination の `required` メンバー（プロパティまたはフィールド。アクセシビリティを問わず、継承したものを含む）がマップされていない（コンストラクタに `[SetsRequiredMembers]` があれば対象外） | エラー |
| SMP0304 | コンストラクタの引数が `required` メンバーを代入していて、オブジェクト初期化子で設定し直すことになる（コンストラクタに `[SetsRequiredMembers]` があれば対象外） | エラー |
| SMP0305 | 戻り値のあるマッパーが destination を作れない（abstract、インターフェイス、マッパーから値を渡して呼べるコンストラクタがない、`new()` / `struct` の制約がない型パラメーター） | エラー |
| SMP0401 | 適用される `Culture` がないのに `DateTimeFormat` / `NumberFormat` を指定している | エラー |
| SMP0402 | AOT 非対応: 汎用 `Convert<TSource,TDest>` フォールバックに到達する可能性がある（クラス・構造体・コレクションでは、`[MapNested]` / `[MapCollection]` を使うようメッセージで案内する） | エラー |
| SMP0403 | AOT 警告: `MapExpression` にリフレクションパターンが含まれる可能性がある | 警告 |
| SMP0404 | `Culture` がカルチャ名ではない | エラー |
| SMP0501 | Strict モード: マッパーから代入できる目的プロパティや、マッパーが呼べるコンストラクタでしか設定できない目的プロパティがマップされていない（マッパーのメソッドの位置で報告） | 警告 |
| SMP0502 | Strict モード: `NullValue`・`NullBehavior.Skip`・`[MapCondition]` なしで、null になりうる値を null を受け付けないターゲットに入れ、ターゲットが `null` か `default` を受け取る（属性の位置、自動マッピングならマッパーのメソッドの位置で報告）。null 許容で宣言した source から null を受け付けない型を返すマッパーも警告する（マッパーのメソッドの位置で、ターゲット `(return)` として報告） | 警告 |
| SMP0503 | Strict モード: 名前で対応させて写すターゲットの enum に、ソースの enum のメンバーと同じ名前のメンバーがない（属性の位置、自動マッピングならマッパーのメソッドの位置で報告） | 警告 |

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

- **`FrozenSet` の直接構築** — 生成コードは `HashSet<T>` を構築してから `ToFrozenSet` を呼ぶ（BCL の設計上の二段構築）。BCL に frozen コレクションのビルダー API が追加されれば、中間セットを排除できる。
- **ジェネリックフォールバック `Convert<TSource, TDestination>` の `Half` / `Int128` / `UInt128` / `BigInteger` ソース対応** — ジェネリックコンバーターへのオプトイン経由では boxing フォールバックに到達する。既定の specialized メソッド経路はカバー済みのため、需要が生じた場合に分岐を追加する。
- **ジェネレーターのインクリメンタリティ** — モデルはマッパーメソッドごとに作ってキャッシュし、ソースはクラスごとに生成してキャッシュするため、編集はマッパーが変わったクラスだけを生成し直す。プロパティの一覧、型の検索、変換器の検索はコンパイルの中で共有する。モデルのクラスごとへの振り分けは、編集のたびにすべてのモデルを通る（`Collect()`）。現状のコストは小さく、非常に大きなプロジェクトで必要になれば、さらに細かい分割を検討する。

---

## ライセンス

MIT
