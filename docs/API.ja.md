# Smart.Mapper API リファレンス

[README](../README.ja.md) | [診断](../Diagnostics.md)（英語） | [English](API.md)

この文書は、Smart.Mapper の属性と、ソースジェネレーターが従う規則を、細かな場合まで含めて説明します。概要と短い例は [README](../README.ja.md) を、それぞれの診断の原因と対処は [Diagnostics.md](../Diagnostics.md)（英語）を参照してください。

## 目次

- [マッパーメソッド](#マッパーメソッド)
  - [static とインスタンスのマッパー](#static-とインスタンスのマッパー) · [void と戻り値のパターン](#void-と戻り値のパターン) · [引数](#引数) · [カスタムパラメーター](#カスタムパラメーター) · [拡張メソッド](#拡張メソッド) · [入れ子の型の中のマッパー](#入れ子の型の中のマッパー) · [ジェネリックのマッパーメソッド](#ジェネリックのマッパーメソッド) · [マッパーが写すもの](#マッパーが写すもの)
- [属性リファレンス](#属性リファレンス)
  - [属性の一覧](#属性の一覧) · [属性に書く名前](#属性に書く名前) · [`[Mapper]`](#mapper) · [`[MapperProfile]`](#mapperprofile) · [`[MapProperty]`](#mapproperty--mappropertyt) · [`[MapIgnore]`](#mapignore) · [`[MapUsing]`](#mapusing) · [`[MapFrom]`](#mapfrom) · [`[MapConstant]`](#mapconstant--mapconstantt) · [`[MapExpression]`](#mapexpression) · [`[MapCondition]`](#mapcondition) · [`[BeforeMap]` / `[AfterMap]`](#beforemap--aftermap) · [`[MapNested]`](#mapnested) · [`[MapCollection]`](#mapcollection) · [`[ValueConverter]`](#valueconverter) · [`[CollectionConverter]`](#collectionconverter) · [列挙型](#列挙型)
- [自動マッピングと名前の照合](#自動マッピングと名前の照合)
  - [自動マッピング](#自動マッピング) · [自動マッピングの無効化](#自動マッピングの無効化) · [名前の比較方式](#名前の比較方式) · [代入の順序](#代入の順序) · [廃止されたメンバー](#廃止されたメンバー)
- [属性が指すメソッド](#属性が指すメソッド)
  - [メソッドの探し方](#メソッドの探し方) · [インスタンスメソッドと static メソッド](#インスタンスメソッドと-static-メソッド) · [値の受け取り方](#値の受け取り方) · [オーバーロード](#オーバーロード) · [戻り値](#戻り値)
- [プロパティパス](#プロパティパス)
  - [Flatten](#flatten) · [Unflatten](#unflatten) · [ドット付きパスと自動マッピング](#ドット付きパスと自動マッピング)
- [子オブジェクト](#子オブジェクト)
- [コレクション](#コレクション)
  - [要素の写し方](#要素の写し方) · [ターゲットのコレクション](#ターゲットのコレクション) · [要素のマッパー](#要素のマッパー) · [独自のコレクションクラス](#独自のコレクションクラス) · [コレクションそのもののマッピング](#コレクションそのもののマッピング) · [コレクション変換器](#コレクション変換器) · [既存のコレクションに詰め直す](#既存のコレクションに詰め直す)
- [コンストラクタと record](#コンストラクタと-record)
  - [コンストラクタの呼び出し](#コンストラクタの呼び出し) · [void マッパーとコンストラクタ](#void-マッパーとコンストラクタ) · [コンストラクタの引数への値](#コンストラクタの引数への値) · [`init` 専用と `required` のメンバー](#init-専用と-required-のメンバー) · [コンストラクタの選び方](#コンストラクタの選び方) · [コンストラクタ引数の変換](#コンストラクタ引数の変換)
- [Null 処理](#null-処理)
  - [null 許容の値](#null-許容の値) · [Null 代替値（`NullValue`）](#null-代替値nullvalue) · [写し先の値を残す（`NullBehavior.Skip`）](#写し先の値を残すnullbehaviorskip) · [null 許容の注釈なし](#null-許容の注釈なし) · [`[MaybeNull]` のメンバー](#maybenull-のメンバー) · [null 許容の引数と戻り値](#null-許容の引数と戻り値) · [作るインスタンスの null 許容注釈](#作るインスタンスの-null-許容注釈)
- [型変換](#型変換)
  - [そのまま代入するもの](#そのまま代入するもの) · [スペシャライズドメソッド](#スペシャライズドメソッド) · [そのほかの変換](#そのほかの変換) · [enum の変換](#enum-の変換) · [既定の書式](#既定の書式) · [`DefaultValueConverter`](#defaultvalueconverter) · [カスタム型変換器](#カスタム型変換器) · [変換器の優先順位](#変換器の優先順位)
- [カルチャと書式](#カルチャと書式)
  - [既定のカルチャ](#既定のカルチャ) · [カルチャ名](#カルチャ名) · [`CultureInfo` の引数](#cultureinfo-の引数) · [カルチャの優先順位](#カルチャの優先順位) · [書式](#書式) · [カルチャを受け取る変換器のオーバーロード](#カルチャを受け取る変換器のオーバーロード)
- [プロファイル](#プロファイル)
- [コールバックと条件](#コールバックと条件)
  - [Before / After コールバック](#before--after-コールバック) · [条件付きマッピング](#条件付きマッピング)
- [Strict モード](#strict-モード)
  - [マップされていないメンバー](#マップされていないメンバー) · [null になりうる値](#null-になりうる値) · [対応するメンバーのない enum のメンバー](#対応するメンバーのない-enum-のメンバー) · [警告の抑制](#警告の抑制)
- [NativeAOT / トリミング](#nativeaot--トリミング)
- [診断](#診断)
  - [診断を報告する位置](#診断を報告する位置) · [診断の一覧](#診断の一覧)

---

## マッパーメソッド

マッパーメソッドは `[Mapper]` を付けた `partial` メソッドで、その実装をジェネレーターがコンパイル時に書きます。ソースのメンバーを destination のメンバーへ写します。void マッパーは渡された destination を埋め、戻り値のあるマッパーは destination を作って返します。

### static とインスタンスのマッパー

`[Mapper]` は static の partial メソッドにも、インスタンス（static でない）の partial メソッドにも付けられます。生成コードは含む型を別のファイルに宣言し直すため、含む型はどれも `partial` で、`file` の型でない必要があります（そうでなければ SMP0001。[入れ子の型の中のマッパー](#入れ子の型の中のマッパー)を参照）。

```csharp
// static のマッパー
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial Destination Map(Source source);
}

// インスタンスのマッパー（コンストラクタで受け取ったサービスを使う）
public sealed partial class OrderMapper
{
    private readonly TimeProvider timeProvider;

    public OrderMapper(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    [Mapper]
    [AfterMap(nameof(Fill))]
    public partial OrderDto Map(Order order);

    private void Fill(Order source, OrderDto destination)
    {
        destination.MappedAt = timeProvider.GetUtcNow();
    }
}
```

インスタンスのマッパーは、属性が指すメソッドを自分のインスタンスで呼ぶため、static メソッドのほか、マッパーのクラスとその基底クラスのインスタンスメソッドも使えます。対象は、`[BeforeMap]`・`[AfterMap]`・`[MapUsing]`・`[MapCondition]` のメソッド、`[MapProperty]` の `Converter`、`[MapNested]` / `[MapCollection]` の `Mapper` です。マッパーのクラスを含むクラスと、`global using static` で取り込んだ型のメソッドは static である必要があります。クラスのインスタンスのマッパーでは `[MapExpression]` の式からもインスタンスのメンバーを使えますが、構造体のものでは使えません。C# は構造体のローカル関数から `this` を使えないため（CS1673）、そこでは `[MapUsing]` のインスタンスメソッドを使ってください。static のマッパーにはインスタンスメソッドを呼ぶインスタンスがないため、インスタンスメソッドを指すと診断されます（SMP0105）。[インスタンスメソッドと static メソッド](#インスタンスメソッドと-static-メソッド)を参照してください。

拡張メソッドのマッパーは、拡張メソッドであるため static のままです（[拡張メソッド](#拡張メソッド)）。

実装は宣言の修飾子を繰り返します。static のマッパーなら `static`、アクセシビリティ、`new`、`virtual`、`override`、`sealed`、`readonly`、`unsafe` です。エラーを報告したマッパーの、例外を投げる実装も同じです（[診断を報告する位置](#診断を報告する位置)）。そのため、インスタンスのマッパーは `virtual` にして派生クラスのマッパーで override でき（`override`、`sealed override`）、構造体の `readonly` のメンバーにもできます。void マッパーは `static partial void Map(Source source, Destination destination);` のようにアクセシビリティ修飾子なしで宣言でき（暗黙に `private`）、実装も同じ形で宣言します。

### void と戻り値のパターン

```csharp
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

生成コード（void パターン）：

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
}
```

生成コード（戻り値パターン）：

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

void マッパーは渡された destination を埋め、destination を作らないため、destination のコンストラクタは影響しません（[void マッパーとコンストラクタ](#void-マッパーとコンストラクタ)）。構造体の destination は、`Map(Source source, ref Destination destination)` のように `ref` で受け取ります。値渡しでは呼び出し元に見えないコピーを埋めることになり、`in` や `ref readonly` ではメンバーに代入できないためです（SMP0005）。

戻り値のあるマッパーは、[コンストラクタと record](#コンストラクタと-record) のとおりに選んだコンストラクタで destination を作り、値で返します。`ref` や `ref readonly` で返すと宣言したものは、参照が作った destination を指すことになるため診断されます（SMP0002）。`Point? Map(Src source)` のように null 許容の構造体を戻り値の型にすると、その中の構造体として作って埋めます。

### 引数

マッパーの最初の引数はソースです。void マッパーはその後に destination を受け取り、それより後の引数は[カスタムパラメーター](#カスタムパラメーター)です。引数のないマッパーや、ソースの後に destination の引数のない void マッパーは診断されます（SMP0003）。

- `__` で始まる引数名は、生成コードが宣言する名前のために予約されています（SMP0004）。
- 引数は `out` にできず、void マッパーの構造体の destination は、値渡し・`in`・`ref readonly` ではなく `ref` で受け取ります（SMP0005）。
- `Map(Point? source)` のように null 許容の値型のソースの引数は、中の構造体のメンバーを持たず（`HasValue` と `Value` だけ）、何も写さないため診断されます（SMP0006）。構造体そのものを受け取り、null は呼び出す前に調べてください。
- `CultureInfo` のカスタムパラメーターが複数あるときは、`culture` という名前のものが変換のカルチャになり、その名前のものがなければ診断されます（SMP0008。[`CultureInfo` の引数](#cultureinfo-の引数)を参照）。

null になりうるソースの引数と、void マッパーの destination の引数は、写す前に検査します（[null 許容の引数と戻り値](#null-許容の引数と戻り値)）。

### カスタムパラメーター

ソース（と void マッパーの destination）より後の引数はカスタムパラメーターです。属性が指すメソッドのうち、それを宣言したもの、つまり `[MapUsing]` のメソッド、`[MapProperty]` の `Converter`、`[MapCondition]` のメソッド、`[BeforeMap]` / `[AfterMap]` のコールバック、`[MapNested]` / `[MapCollection]` のマッパーに渡します。`[MapExpression]` の式からは名前で参照できます。

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

メソッドは、通常の引数の後に宣言したカスタムパラメーターを受け取ります。どれを宣言してもよく、順番も問いません。引数はそれぞれ同じ型のカスタムパラメーターを受け取り、マッパーかメソッドにその型の引数が複数あるときは、名前が同じものを受け取ります。型は、C# が同一性の変換をするものどうしを同じとみなし、null 許容の注釈やタプルの要素の名前は問いません（`(int X, string Y)` は `(int, string)` に渡ります）。`dynamic` は `object`、`nint` は `IntPtr` と同じです。受け取るものがない引数（その型のカスタムパラメーターがない、または複数のうち名前が同じものがない）を持つメソッドは一致しません。オーバーロードは、カスタムパラメーターを多く受け取るものを先にします。マッパーは同じ型のカスタムパラメーターを複数持てます。コレクション変換器にデリゲートとして渡す要素のマッパー（[コレクション変換器](#コレクション変換器)）と、`[ValueConverter]` / `[CollectionConverter]` のクラスのメソッドには渡りません。カスタムパラメーターは null を調べずにそのまま渡すため、null 許容で宣言したものを null を受け付けない引数に渡すと、生成コードで null 許容の警告（CS8604）が出ます。ただし、変換のカルチャを決める `CultureInfo` の引数は、そのような引数には変換が使うカルチャとして渡します（[`CultureInfo` の引数](#cultureinfo-の引数)）。

```csharp
[Mapper]
[MapProperty(nameof(Destination.Title), Converter = nameof(Enclose))]
public static partial Destination Map(Source source, string open, string close);

// 同じ型の 2 つを名前で、自分の順番で受け取る
private static string Enclose(string value, string close, string open) => open + value + close;
```

呼び出せるオーバーロードのうち、カスタムパラメーターをもっとも多く受け取るものの中では、同じものを受け取るオーバーロードから、C# と同じように呼び出しが結び付くものを使います（[オーバーロード](#オーバーロード)）。両方を持つマッパーに対する `Conv(string value, Ctx ctx)` と `Conv(string value, Other other)` のように、同じ数の違うものを受け取るオーバーロードは、どれかを選ぶ手がかりがないため、宣言の順によらずどれも使わず、受け取る数の少ないオーバーロードも使いません。その属性は、一致しないメソッドを指すものとして診断されます（変換器は SMP0110、条件は SMP0112、`[MapUsing]` のメソッドは SMP0201、コールバックは SMP0106 / SMP0107、`[MapCollection]` / `[MapNested]` のマッパーは SMP0213 / SMP0214）。

`System.Globalization.CultureInfo` 型のカスタムパラメーターは、そのメソッドの変換のカルチャも決めます（[`CultureInfo` の引数](#cultureinfo-の引数)）。

1.0.0-beta12 までは、メソッドはカスタムパラメーターをすべて、マッパーと同じ順番で宣言したときだけ受け取り、`[MapNested]` / `[MapCollection]` のマッパーはどれも受け取りませんでした。今はどれを受け取ってもよく、多く受け取るものが先になるため、一部を受け取る既存のオーバーロードが、1.0.0-beta12 では受け取らないものを呼んでいたところで呼ばれます。`Map(Source source, A a, B b)` に変換器 `Conv(string value)` と `Conv(string value, A a)` があれば、1.0.0-beta12 は `Conv(value)` を呼び、今は `Conv(value, a)` を呼びます。コールバックや、子と要素のマッパーも同じです。

### 拡張メソッド

`[Mapper]` メソッドは、static である拡張メソッドとして宣言できます。生成される実装側の宣言にも `this` が付くため、呼び出し側は自然に書けます。

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

destination の型パラメーターは `new T()` で作るため、`new()` か `struct` の制約が必要です（そうでなければ SMP0303）。

### マッパーが写すもの

マッパーはオブジェクトを別のオブジェクトへ写します。ソースや destination がコレクション・配列・配列やメモリーの要素のビュー（`Span<T>`・`Memory<T>`・`ArraySegment<T>` など）・タプル（またはそれに制約された型パラメーター）のマッパーは、コレクションのメンバーを写すだけで要素をひとつも写さないため、診断されます（SMP0007）。要素は要素の型のマッパーで写すか、コレクションを持つ型を `[MapCollection]` で写してください（[コレクションそのもののマッピング](#コレクションそのもののマッピング)）。

---

## 属性リファレンス

### 属性の一覧

| 属性 | 付ける場所 | 説明 |
|------|-----------|------|
| [`[Mapper]`](#mapper) | メソッド | マッパーメソッドの指定。`AutoMap`・`Strict`・`NameComparison`・`Culture`・`DateTimeFormat`・`NumberFormat` |
| [`[MapperProfile]`](#mapperprofile) | アセンブリ・クラス・構造体 | マッパーメソッドの既定値。`Strict`・`NameComparison`・`DefaultCulture`・`Culture`・`DateTimeFormat`・`NumberFormat` |
| [`[MapProperty]`](#mapproperty--mappropertyt) / `[MapProperty<T>]` | メソッド・複数可 | ソースのメンバーやドット付きパスからターゲットへのマッピング。`Converter`・`NullValue`・`NullBehavior`・`Culture`・`DateTimeFormat`・`NumberFormat`・`Order` |
| [`[MapIgnore]`](#mapignore) | メソッド・複数可 | destination のメンバーを自動マッピングから除外 |
| [`[MapUsing]`](#mapusing) | メソッド・複数可 | メソッドがソースから計算した値を代入（カスタムパラメーター対応） |
| [`[MapFrom]`](#mapfrom) | メソッド・複数可 | ソースの引数なしメソッドの結果、またはプロパティパスを代入 |
| [`[MapConstant]`](#mapconstant--mapconstantt) / `[MapConstant<T>]` | メソッド・複数可 | 固定値を代入 |
| [`[MapExpression]`](#mapexpression) | メソッド・複数可 | C# の式の値を代入（例: `"System.DateTime.Now"`） |
| [`[MapCondition]`](#mapcondition) | メソッド・複数可 | 条件メソッドが `true` を返したときだけターゲットをマッピング |
| [`[BeforeMap]` / `[AfterMap]`](#beforemap--aftermap) | メソッド | マッピングの前 / 後にメソッドを呼ぶ |
| [`[MapNested]`](#mapnested) | メソッド・複数可 | マッパーメソッドでメンバーをマッピング |
| [`[MapCollection]`](#mapcollection) | メソッド・複数可 | マッパーメソッドでコレクションのメンバーを要素ごとにマッピング。`Strategy`・`Converter` |
| [`[ValueConverter]`](#valueconverter) | クラス・構造体・メソッド | カスタム型変換器のクラス。`Method` |
| [`[CollectionConverter]`](#collectionconverter) | クラス・構造体・メソッド | カスタムコレクション変換器のクラス |

`[MapProperty<T>]` と `[MapConstant<T>]` は `[MapProperty]` と `[MapConstant]` の型安全版で、ジェネリックの属性のため C# 11 以降が必要です。

### 属性に書く名前

- **第1引数の規則** - destination のメンバーをマッピングする属性では、第1引数は **destination**（ターゲット）名です。第2引数は `[MapProperty]`・`[MapFrom]`・`[MapCollection]`・`[MapNested]` では source、`[MapUsing]`・`[MapCondition]`・`[MapConstant]`・`[MapExpression]` ではメソッド・定数・式です。
- メンバーの名前はメソッドの `NameComparison` で比較し、メソッドの名前は C# の識別子として完全一致で探します（[名前の比較方式](#名前の比較方式)）。
- ターゲットには、戻り値のあるマッパーが呼ぶコンストラクタの引数名も書けます（[コンストラクタの引数への値](#コンストラクタの引数への値)）。
- **キーワードの名前** - `@class` のように C# のキーワードである名前は、生成コードでも `@` を付けて書かれます。そのような名前のメンバー・enum のメンバー・引数・メソッド・クラス・名前空間も、ほかと同じようにマッピングできます。

### `[Mapper]`

partial メソッド（static でもインスタンスでも）をマッパーとして指定します（[マッパーメソッド](#マッパーメソッド)）。

| プロパティ | 型 | 既定値 | 説明 |
|-----------|----|--------|------|
| `AutoMap` | `bool` | `true` | 同じ名前のメンバーを自動でマッピングする。`false` なら属性で指定したものだけ（[自動マッピングの無効化](#自動マッピングの無効化)） |
| `Strict` | `bool` | `false` | 未マップの destination のメンバー（SMP0501）、null を受け付けないターゲットへの null になりうる値（SMP0502）、ターゲットの enum に同じ名前のメンバーがない enum のメンバー（SMP0503）を警告（[Strict モード](#strict-モード)） |
| `NameComparison` | `StringComparison` | `Ordinal` | メンバー名の比較方式。自動マッピングとマッピング属性に書いた名前の双方に適用（[名前の比較方式](#名前の比較方式)） |
| `Culture` | `string?` | `null` | 変換のカルチャ名（例: `"ja-JP"`）（[カルチャと書式](#カルチャと書式)） |
| `DateTimeFormat` | `string?` | `null` | 日付と時刻の型と `string` の間の変換の書式（[書式](#書式)） |
| `NumberFormat` | `string?` | `null` | 数値と `string` の間の変換の書式（[書式](#書式)） |

`Strict`・`NameComparison`・`Culture`・`DateTimeFormat`・`NumberFormat` の既定値は[プロファイル](#プロファイル)から取ります。`[Mapper]` で指定した値が優先され（strict のプロファイルの下での `Strict = false` も）、次にクラスのプロファイル、次にアセンブリのプロファイルの値で、設定ごとに独立に決まります。

null を受け付けない `CultureInfo` の引数を持つメソッドの `Culture` は、引数がカルチャを決めるため使われず、警告として診断されます（SMP0404）。null を受け付ける引数（`CultureInfo?`、または null 許容の注釈なしで宣言したもの）は、null の引数ではそのカルチャを使うため、診断されません（[`CultureInfo` の引数](#cultureinfo-の引数)）。

### `[MapperProfile]`

クラスや構造体、またはアセンブリのマッパーメソッドの既定値を指定します（[プロファイル](#プロファイル)）。

| プロパティ | 型 | 既定値 | 説明 |
|-----------|----|--------|------|
| `Strict` | `bool` | `false` | `[Mapper]` の `Strict` の既定値 |
| `NameComparison` | `StringComparison` | `Ordinal` | `[Mapper]` の `NameComparison` の既定値 |
| `DefaultCulture` | `MapperCulture` | `Invariant` | カルチャ名が当てはまらないときのカルチャ。`Invariant` か `Current`（[既定のカルチャ](#既定のカルチャ)） |
| `Culture` | `string?` | `null` | `[Mapper]` の `Culture` の既定値 |
| `DateTimeFormat` | `string?` | `null` | `[Mapper]` の `DateTimeFormat` の既定値 |
| `NumberFormat` | `string?` | `null` | `[Mapper]` の `NumberFormat` の既定値 |

### `[MapProperty]` / `[MapProperty<T>]`

`[MapProperty(target)]` と `[MapProperty(target, source)]` は、ソースのメンバーからターゲットへマッピングします。

| メンバー | 型 | 既定値 | 説明 |
|---------|----|--------|------|
| `Target` | `string` | | destination のメンバー、その中へのドット付きパス（`Child.Value`）、またはコンストラクタの引数 |
| `Source` | `string?` | ターゲット名 | ソースのメンバー、またはドット付きパス（`Child.Name`） |
| `Converter` | `string?` | `null` | ソースの値を変換するメソッド（[属性が指すメソッド](#属性が指すメソッド)） |
| `NullValue` | `object?`（`[MapProperty<T>]` では `T`） | 指定なし | ソースが null のときにターゲットが受け取る値（[`NullValue`](#null-代替値nullvalue)） |
| `NullBehavior` | `NullBehavior` | `Default` | `Skip` ならソースが null のときターゲットをそのまま残す（[`NullBehavior.Skip`](#写し先の値を残すnullbehaviorskip)） |
| `Culture` | `string?` | `null` | このマッピングのカルチャ名。`CultureInfo` の引数とメソッドのカルチャより優先（[カルチャの優先順位](#カルチャの優先順位)） |
| `DateTimeFormat` | `string?` | `null` | このマッピングの日付と時刻の書式（[書式](#書式)） |
| `NumberFormat` | `string?` | `null` | このマッピングの数値の書式（[書式](#書式)） |
| `Order` | `int` | `0` | 同じ種類の代入の中での順序（[代入の順序](#代入の順序)） |

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

- ドット付きのソースは途中のメンバーを null チェックの下で読み、ドット付きのターゲットは通るメンバーの中へ書き込みます（[プロパティパス](#プロパティパス)）。
- 解決できない名前は無視されず診断されます。ソース側（見つからない、マッパーのクラスから呼べる getter がない、パスの途中も含めエラー扱いの `[Obsolete]`）は SMP0108、ターゲット側は SMP0102 です。
- 同じターゲットをマッピングする 2 つの属性や、ある属性でメンバー全体を、別の属性でその中をドット付きパスでマッピングするもの（`Child` と `Child.Value`）は、両方は成り立たないため診断されます（SMP0101）。
- 値は[型変換](#型変換)のとおりにターゲットの型へ変換し、[カルチャと書式](#カルチャと書式)のカルチャと書式を使います。

### `[MapIgnore]`

`[MapIgnore(target)]` は、destination のメンバー全体であるターゲットを、自動マッピングから除外します。

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

- ドット付きのターゲット（`Child.Value`）は診断されます（SMP0103）。自動マッピングはメンバーの中のメンバーを単独で代入しないため、除外するものがありません。
- `[MapIgnore]` と、同じターゲットをマッピングする属性は矛盾するため、属性の種類を問わず診断されます（SMP0101）。メンバーを `[MapIgnore]` で除外し、その中へドット付きパスでマッピングするのは許されます。メンバーは自動マッピングから外れ、パスはマッピングされます。
- 見つからないターゲットは診断されます（SMP0102）。ターゲットは destination のプロパティやフィールド、または戻り値のあるマッパーが呼ぶコンストラクタの引数です。
- 戻り値のあるマッパーのコンストラクタの引数が代入するメンバーや、同じ名前のメンバーがない引数も除外できます。その引数には値がないため、別のコンストラクタを選ぶか、省略可能な引数なら渡さず、引数なしで作れる型なら引数なしで作ります（[コンストラクタの選び方](#コンストラクタの選び方)）。ほかに作る方法がない destination は診断されます（SMP0304）。
- 戻り値のあるマッパーが作る destination の `required` メンバーは、構築時に設定する必要があるため除外できません（SMP0304）。呼ぶコンストラクタに `[SetsRequiredMembers]` があれば除外できます。
- Strict モードでは、省いた省略可能なコンストラクタ引数が設定するはずのメンバーを `[MapIgnore]` で指せば、既定値に任せたまま警告を消せます（[マップされていないメンバー](#マップされていないメンバー)）。

### `[MapUsing]`

`[MapUsing(target, method)]` は、メソッドがソースから計算した値をターゲットに代入します。

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

- メソッドはソースを受け取り、宣言していればその後に[カスタムパラメーター](#カスタムパラメーター)を受け取ります。[属性が指すメソッド](#属性が指すメソッド)のとおりに探して照合します。インスタンスのマッパーではインスタンスメソッドでもよく、ジェネリックメソッドは使わず、ソースをその型のまま、または値渡しなら暗黙に変換できる型として受け取ります（そうでなければ SMP0201。あいまいな呼び出しも同じ）。
- 返す型は、ターゲットの型か、代入と同じく C# が暗黙に変換できる型です。`long` や `int?` のターゲットに `int`、基底クラスや実装するインターフェイスのターゲットにそのクラスを返せます。`int` のターゲットに `long` のように、明示的な変換が要る型は診断されます（SMP0202）。null 許容の参照を返し、ターゲットが null 許容でないときは、`[MapFrom]` と同じく `!` を付けて受け取ります。ただし、最初の引数を指定した `[return: NotNullIfNotNull]` で、マッパーの null 検査を通った source には null でない値を返すと分かるときは、そのまま受け取ります。
- ターゲットは、マッパーから代入できるプロパティやフィールドです（`"Child.Value"` のようなドット付きパスも使え、途中のメンバーは `[MapProperty]` のパスと同じように扱います。[Unflatten](#unflatten) を参照）。メソッドは、ドット付きパスやフィールドのターゲットでも、その型で照合されます。見つからないもの（綴りの誤り、メソッド、static メンバー）や代入できないもの（`readonly` フィールド）は診断されます（SMP0102）。
- 戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、値を引数としてコンストラクタに渡し、メソッドはその引数の型を返します（[コンストラクタの引数への値](#コンストラクタの引数への値)）。
- ドット付きのターゲットはそのメンバーの中へ書き込み、そのメンバーは自動マッピングから外れます。コンストラクタが代入するメンバーの中へは書けず（SMP0301）、null 許容の構造体も通れません。`Value` は写しで、書き戻すセッターがないためです（SMP0102）。
- `[MapUsing]` のターゲットはプロパティマッピングではなくメソッドが代入するため、そこへの `[MapCondition]` は診断されます（SMP0109）。

### `[MapFrom]`

`[MapFrom(target, member)]` は、ソースのメンバー（引数なしのメソッド、またはプロパティパス）からターゲットに代入します。

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // インスタンスメソッド呼び出し
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // ドット記法パス
public static partial void Map(Source source, Destination destination);
```

- メソッドは、マッパーのクラスから呼べる、ソースの引数なしのインスタンスメソッドです（ジェネリックメソッドは除きます）。`source.Method()` の呼び出しと同じように探すため、基底クラスのもの、ソースがインターフェイスならそれが継承するインターフェイスのものも見つかります。派生側のものが先で、`new` で隠したメソッドは派生側が使われます。`protected` や `private` のものは見つかりません。
- プロパティパスは、マッパーのクラスから呼べる getter のあるプロパティを、継承したものも含めてたどります。
- 見つからないメンバーは診断されます（SMP0204）。警告扱いの `[Obsolete]` のメンバーは使い、エラー扱いのものは診断されます（SMP0204）。
- メンバーの型は、ターゲットの型か、`[MapUsing]` のメソッドと同じく暗黙に変換できる型です（そうでなければ SMP0205）。
- null になりうるメンバーを通るプロパティパスは、`[MapProperty]` のソースのパスと同じく、その null 検査の下で読みます（null 許容の構造体は中の構造体を通して読みます。`Location.Lat` なら `source.Location.Value.Lat`）。途中が null のときはターゲットをそのまま残し、コンストラクタの引数やオブジェクト初期化子の項目では、null を受け取るターゲットには `null`、それ以外には `default` を渡します。
- null 許容の参照を null 許容でないターゲットに写すときは、`[MapProperty]` と同じく `!` を付けます。プロパティ、その getter、メソッドの戻り値に `[MaybeNull]` が付いた値も同じです。
- destination にないターゲットは診断され（SMP0203）、マッパーが代入できないターゲットも診断されます（SMP0102）。戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、値を引数の型でコンストラクタに渡します。

```csharp
// [MapFrom(nameof(Destination.City), "Customer.Address.City")]、Customer と Address は null 許容
if (source.Customer is not null && source.Customer.Address is not null)
{
    destination.City = source.Customer.Address.City;
}
```

### `[MapConstant]` / `[MapConstant<T>]`

`[MapConstant(target, value)]` と `[MapConstant<T>(target, value)]` は、固定値をターゲットに代入します。

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

非 Generic 版: `[MapConstant("Status", "Active")]`。マッピングのときに計算する値には、`[MapExpression("CreatedAt", "System.DateTime.Now")]` のように `[MapExpression]` を使います。

定数はその型の式として書かれます。enum はメンバー（フラグの組み合わせのようにメンバーのない値はキャスト）、型は `typeof`、配列はマッピングごとに作る新しい配列、数値・文字・文字列はどのカルチャでも同じ綴りの C# リテラルです（`double.NaN` や無限大は名前で書き、引用符や制御文字はエスケープします）。リテラルを持たない `byte`・`sbyte`・`short`・`ushort` はキャストで書くため、どんな値も受け取るターゲットでも型を保ちます（`object` には `int` ではなく `short` として箱詰めされます）。`[Obsolete]` の enum メンバーは、同じ値の廃止されていないメンバーがあればその名前で、なければ値のキャストで書きます（[廃止されたメンバー](#廃止されたメンバー)）。

```csharp
[MapConstant(nameof(Destination.Kind), Kind.Active)]                  // __d.Kind = global::Sample.Kind.Active;
[MapConstant(nameof(Destination.Access), Access.Read | Access.Write)] // __d.Access = (global::Sample.Access)3;
[MapConstant(nameof(Destination.ItemType), typeof(Item))]             // __d.ItemType = typeof(global::Sample.Item);
[MapConstant(nameof(Destination.Codes), new[] { 1, 2 })]              // __d.Codes = new int[] { 1, 2 };
[MapConstant(nameof(Destination.Ratio), 0.1)]                         // __d.Ratio = 0.1d;
[MapConstant(nameof(Destination.Flag), (short)-1)]                    // __d.Flag = (short)-1;
[MapConstant(nameof(Destination.Note), "tab\there")]                  // __d.Note = "tab\there";
```

- 値はコンパイラーの変換規則でターゲットの型に変換できる必要があります（`1` を `long` や `byte` へ、`null` を参照型へ、など）。`int` への `"abc"`、`float` への `1.5`、`byte` への `short`、数値や別の enum への enum、null を許さない参照型への `null`（や `null` を含む配列）は診断されます（SMP0216）。ファイルローカル型のように生成コードから参照できない値も診断されます（SMP0215）。
- ターゲットは、マッパーから代入できるプロパティやフィールドです（`"Child.Value"` のようなドット付きパスも使え、途中のメンバーは `[MapProperty]` のパスと同じように扱います。[Unflatten](#unflatten) を参照）。見つからないもの（綴りの誤り、メソッド、static メンバー）や代入できないもの（`readonly` フィールド）は診断されます（SMP0102）。ドット付きのターゲットはそのメンバーの中へ書き込み、そのメンバーは自動マッピングから外れます。コンストラクタが代入するメンバーの中へは書けず（SMP0301）、null 許容の構造体も通れません。`Value` は写しで、書き戻すセッターがないためです（SMP0102）。
- 戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、値を引数としてコンストラクタに渡し、引数の型に対して確かめます。

### `[MapExpression]`

`[MapExpression(target, expression)]` は、C# の式の値をターゲットに代入します。

```csharp
[Mapper]
[MapExpression(nameof(Destination.CreatedAt), "System.DateTime.Now")]
[MapExpression(nameof(Destination.Total), "source.Price * source.Quantity")]
public static partial void Map(Source source, Destination destination);
```

- 式は、マッパーの引数を同じ名前で受け取るローカル関数としてコンパイルされます。そのため式から引数や[カスタムパラメーター](#カスタムパラメーター)を参照でき（例: `"source.Price * source.Quantity"`）、`out var` やパターンで宣言した変数がほかの式と衝突しません。関数はターゲットの型を返すため、式は直接代入したときと同じようにターゲットへ変換されます。
- static のマッパーでは、関数は static です。インスタンスのマッパーでは static でないため、クラスなら式からマッパーのクラスのインスタンスのメンバー（フィールド・プロパティ・メソッド）も使えます。構造体では、C# が構造体のローカル関数から `this` を使えないため（CS1673）使えません。そのような値は `[MapUsing]` のインスタンスメソッドで計算してください。
- ターゲットは `[MapConstant]` のターゲットと同じ規則です。マッパーから代入できるプロパティやフィールドで、ドット付きパスも使えます（SMP0102、SMP0301）。戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、値を引数の型の値としてコンストラクタに渡します。
- リフレクションの API（`Activator`・`Type.GetType`・`MethodInfo` など）を含む式は、AOT に対応しない可能性があるため警告として診断されます（SMP0403。[NativeAOT / トリミング](#nativeaot--トリミング)）。

### `[MapCondition]`

`[MapCondition(target, condition)]` は、条件メソッドがソースの値に対して `true` を返したときだけターゲットに代入します（[条件付きマッピング](#条件付きマッピング)）。

| メンバー | 型 | 説明 |
|---------|----|------|
| `Target` | `string` | 条件が守るプロパティマッピングの destination のメンバー。ドット付きパスも可 |
| `Condition` | `string` | ソースの値（とカスタムパラメーター）を受け取り `bool` を返すメソッド |

### `[BeforeMap]` / `[AfterMap]`

`[BeforeMap(method)]` と `[AfterMap(method)]` は、マッピングの前と後に、ソースと destination を渡してメソッドを呼びます（[Before / After コールバック](#before--after-コールバック)）。どちらもマッパーメソッドごとに 1 つです。

| メンバー | 型 | 説明 |
|---------|----|------|
| `Method` | `string` | ソースと destination、宣言していればその後にカスタムパラメーターを受け取るメソッド |

### `[MapNested]`

`[MapNested(target)]` と `[MapNested(target, source)]` は、マッパーメソッドでターゲットをマッピングします（[子オブジェクト](#子オブジェクト)）。

| メンバー | 型 | 既定値 | 説明 |
|---------|----|--------|------|
| `Target` | `string` | | destination のメンバー、またはコンストラクタの引数 |
| `Source` | `string?` | ターゲット名 | ソースのプロパティ（ドット付きパスは不可） |
| `Mapper` | `string?` | `null` | マッパーメソッド。必須（ないときは SMP0214） |
| `Order` | `int` | `0` | `[MapNested]` の代入の中での順序（[代入の順序](#代入の順序)） |

### `[MapCollection]`

`[MapCollection(target)]` と `[MapCollection(target, source)]` は、マッパーメソッドでコレクションのメンバーを要素ごとにマッピングします（[コレクション](#コレクション)）。

| メンバー | 型 | 既定値 | 説明 |
|---------|----|--------|------|
| `Target` | `string` | | destination のメンバー、またはコンストラクタの引数 |
| `Source` | `string?` | ターゲット名 | ソースのプロパティ（ドット付きパスは不可） |
| `Mapper` | `string?` | `null` | 要素のマッパーメソッド。必須（ないときは SMP0213） |
| `Converter` | `string?` | `null` | ターゲットの型で決まるメソッドの代わりに呼ぶ、コレクション変換器のメソッド（[コレクション変換器](#コレクション変換器)） |
| `Strategy` | `CollectionStrategy` | `Replace` | `Replace` は新しいコレクションを代入し、`InPlace` は既存のコレクションを空にして詰め直す（[既存のコレクションに詰め直す](#既存のコレクションに詰め直す)） |
| `Order` | `int` | `0` | `[MapCollection]` の代入の中での順序（[代入の順序](#代入の順序)） |

### `[ValueConverter]`

`[ValueConverter(typeof(...))]` は、マッパーメソッドに、またはクラスや構造体に付けてそのすべてのマッパーメソッドに、値の変換で呼ぶクラスを指定します（[カスタム型変換器](#カスタム型変換器)）。

| メンバー | 型 | 既定値 | 説明 |
|---------|----|--------|------|
| `ConverterType` | `Type` | | 変換器のクラス。入れ子や総称型でもよい |
| `Method` | `string` | `"Convert"` | メソッドを探す名前。スペシャライズドメソッドは `{Method}To{TargetType}`、汎用のフォールバックは `{Method}<TSource, TDestination>` |

### `[CollectionConverter]`

`[CollectionConverter(typeof(...))]` は、マッパーメソッドに、またはクラスや構造体に付けてそのすべてのマッパーメソッドに、`[MapCollection]` のターゲットのコレクションを作るメソッドを持つクラスを指定します（[コレクション変換器](#コレクション変換器)）。

| メンバー | 型 | 説明 |
|---------|----|------|
| `ConverterType` | `Type` | コレクション変換器のクラス |

### 列挙型

| 列挙型 | メンバー | 説明 |
|-------|---------|------|
| `NullBehavior` | `Default`（0）、`Skip`（1） | `[MapProperty]` のソースが null のときのターゲットの扱い。`Default` はそのときの値（`NullValue`・`null`・`default`）を代入し、`Skip` はターゲットをそのまま残す |
| `CollectionStrategy` | `Replace`（0）、`InPlace`（1） | `[MapCollection]` のターゲットの設定方法。`Replace` は新しいコレクションを代入し（既定）、`InPlace` は既存のインスタンスを空にして写した要素を追加し、ほかから（データバインディングなどで）参照されているインスタンスを保つ |
| `MapperCulture` | `Invariant`（0）、`Current`（1） | カルチャ名が当てはまらないときに変換が使うカルチャ（[既定のカルチャ](#既定のカルチャ)） |

---

## 自動マッピングと名前の照合

### 自動マッピング

同名・互換型のプロパティは自動的にマッピングされます。

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);
```

マッパーから代入できないプロパティ（get だけ、`private set` のようにマッパーから呼べないセッター）は、構築で呼ぶコンストラクタが受け取るものを除き、対象から外します（[コンストラクタと record](#コンストラクタと-record)）。マッピング属性で指定した場合は診断されます（SMP0102）。void マッパーは構築しないため、コンストラクタでしか代入されないメンバーは、get だけのプロパティと同じく対象から外れます。

型が継承するプロパティも対象です。基底クラスのもの、インターフェイスならそれが継承するインターフェイスのものを含みます。名前は、生成コードの `x.Name` が指すもの、つまりマッパーのクラスからアクセスできる、その名前の最も派生したメンバーとして扱います。基底のプロパティを override したものや `new` で隠したものがその名前を表し、getter だけを override したものには、継承した setter で代入します。名前が指すメンバーが public のインスタンスプロパティでない（`internal` のプロパティ、フィールド、メソッド）なら、その名前は自動では写さず、隠された基底のプロパティも写しません。`private` のメンバーはマッパーのクラスから見えないため、何も隠しません。インターフェイスが、互いに隠さない 2 つのインターフェイスから同じ名前を継承していると、その名前はあいまいなため写しません。インデクサーは対象外です。マッピング属性に書いた名前も同じです。ソースのプロパティは getter で読むため、マッパーのクラスから呼べる getter がないもの（`private get` や set だけのもの）はソースになりません。

`[Obsolete]` のプロパティは対象から外れ（[廃止されたメンバー](#廃止されたメンバー)）、ドット付きのターゲットのパスが中へ書き込むメンバーは、全体ではなくそのパスで写します（[ドット付きパスと自動マッピング](#ドット付きパスと自動マッピング)）。

### 自動マッピングの無効化

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]
public static partial void Map(Source source, Destination destination);
// Id のみマッピング。他のプロパティは無視。
```

### 名前の比較方式

`NameComparison` は自動マッピングだけでなく、**マッピング属性に書いた名前**にも適用されます。比較方式は `[Mapper]` の指定、なければクラスの `[MapperProfile]` の指定、次にアセンブリの `[MapperProfile]` の指定、どれもなければ `Ordinal` です。完全一致が常に優先され、設定した比較方式はフォールバックとしてのみ使われるため、既定（`Ordinal`）では完全一致で照合します。

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

マッピング属性に書いたメンバーの名前はすべて対象です。ターゲット（プロパティもフィールドも、ドット付きパスの各段も。`[MapIgnore]` のようにターゲット名のみを取る属性を含む）、ソース、`[MapFrom]` のメンバーです。大文字小文字を無視して複数が当たるときは、先に宣言されたもの（プロパティ、次にフィールド）が選ばれます。メソッドの名前（`Converter`・`[MapCondition]`・`[MapUsing]`・`[BeforeMap]` / `[AfterMap]`・`[MapCollection]` / `[MapNested]` のマッパー）は C# の識別子として完全一致で探します。比較方式で一致しない綴りの名前は、見つからない名前として診断されます（SMP0102、SMP0108）。

### 代入の順序

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

### 廃止されたメンバー

自動マッピングは、`[Obsolete]` のプロパティを、警告扱いでもエラー扱いでも、ソースとしても destination としても写しません。プロパティそのもの、読み書きに使うアクセサー、override の元のプロパティのどれに付いていても同じです（C# は override の元のものを報告します）。Strict モードでも知らせませんが、`required` のものはマッピングが必要です（SMP0308）。

属性で名前を書いて指したメンバーは、警告扱いなら使い（C# が CS0618 を報告します）、エラー扱いなら生成コードが使えない（CS0619）ため、その属性の診断で報告します。対象は、プロパティとフィールド、ドット付きパスの各段、`[MapFrom]` のメソッドです（ソースなら SMP0108・SMP0204・SMP0206、ターゲットなら SMP0102）。属性が指すメソッドも、エラー扱いなら一致しないものとして扱います。`Converter`（SMP0110）、`[MapCondition]` のメソッド（SMP0112）、`[MapUsing]` のメソッド（SMP0201）、`[BeforeMap]` / `[AfterMap]` のコールバック（SMP0106 / SMP0107）、`[MapCollection]` / `[MapNested]` のマッパーメソッド（SMP0213 / SMP0214）です。

エラー扱いのコンストラクタは呼ばず、警告扱いのものはほかに方法がないときだけ呼びます（[コンストラクタの選び方](#コンストラクタの選び方)）。ドット付きのターゲットの途中のメンバーは、`new T()` が結び付くコンストラクタを呼べるときに作ります。それがエラー扱いなら、その型は作れないものとして扱い、destination が持つメンバーに書き込みます。警告扱いなら、ほかに作る方法がないため呼びます（警告が出ます）。

生成コードが自動の変換で呼ぶメンバーも、エラー扱いの `[Obsolete]` なら呼びません。対象は、変換演算子（`implicit` / `explicit`）、`[ValueConverter]` / `[CollectionConverter]` のクラスのメソッド、変換で使う `ToString(format, provider)` と `Parse`、コレクションのクラスを作るコンストラクタです。ほかの変換があればそれを使い（専用のメソッドの代わりに変換クラスの汎用のメソッド、span を受け取る `Parse` の代わりに `string` を受け取るもの）、なければ、変換がない（SMP0402）、変換メソッドが一致しない（SMP0110）、生成コードが作れないコレクション（SMP0212）として報告します。警告扱いなら呼びます（CS0618 が出ます）。

`[Obsolete]` の enum メンバーは、警告扱いでもエラー扱いでも、enum から別の enum（メンバーは従来どおり名前で対応付けます）や文字列への変換、文字列からの変換の switch と、`[MapConstant]` や `NullValue` の enum の定数（同じ値の廃止されていないメンバーがあればその名前で書きます）では、生成コードが警告もエラーも出さないよう、`(Color)2` のように値のキャストで書きます。

---

## 属性が指すメソッド

`[MapUsing]`・`[MapCondition]`・`[BeforeMap]` / `[AfterMap]` のメソッド、`[MapProperty]` の `Converter`、`[MapNested]` / `[MapCollection]` の `Mapper` は、生成コードが修飾なしの名前で、型引数なしで呼びます。名前は C# の識別子として完全一致で探します。ジェネリックメソッドは使わず（一致しないメソッドとして診断されます）、エラー扱いの `[Obsolete]` のもの（[廃止されたメンバー](#廃止されたメンバー)）や、生成コードが渡す引数を修飾子が受け取れないもの（プロパティの値に対する `ref` など。渡す引数は [Diagnostics.md](../Diagnostics.md) に一覧があります）も使いません。

### メソッドの探し方

C# が呼び出しの名前を探すのと同じように探します。マッパーのクラスとその基底クラス（マッパーのクラスから呼べる `protected` のメソッドを含む）を探し、どちらにも呼び出せるその名前のメンバーがなければ、マッパーのクラスを含むクラスとその基底クラス、さらにその外側と、順に探します。最後に、生成コードのファイルからも見える `global using static` で取り込んだ型を探します（1 つのファイルだけの `using static` は見えません。取り込んだ型からは、その型で宣言したメソッドだけを使い、継承したものや拡張メソッドは使いません）。C# と同じく、マッパーのクラスから使え、呼び出せるその名前のメンバーを持つ最初のクラスだけを探します。そのメソッドが引数を受け取れないときも、外側は探しません。呼び出せないメンバー（デリゲートでないプロパティやフィールド、入れ子の型）は飛ばします。派生クラスのメソッドは、同じシグネチャの基底クラスのメソッドを隠します。

マッパーの引数（ソース、destination、カスタムパラメーター）は、C# が最初に見つけるため、これらのどれよりも先になります。その名前で呼ぶと引数を使うことになり、デリゲート型の引数ならメソッドの代わりに呼ばれ、ほかの型ならエラーになります（CS0149）。そのような名前は、それを指す最初の属性の位置で診断されます（SMP0104）。引数かメソッドの名前を変えてください。

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

複数のマッパーのクラスで共通に使うメソッドは、`global using static` で取り込むクラス（プロジェクトの 1 つのファイルに `global using static MyApp.Converters;`）か、マッパーのクラスが継承する基底クラスに置けます。プロパティごとに名前で指すのではなく、元とターゲットの型で決まる変換は、`[ValueConverter]` のクラスに置きます（[カスタム型変換器](#カスタム型変換器)）。

### インスタンスメソッドと static メソッド

static のマッパーは static メソッドだけを呼びます。インスタンスのマッパーは、マッパーのクラスとその基底クラスのメソッドを自分のインスタンスで呼ぶため、static メソッドのほかインスタンスメソッドも使え、コンストラクタで受け取ったサービスなどインスタンスのフィールドを使えます。マッパーのクラスを含むクラスと `global using static` で取り込んだ型のメソッドは、呼ぶためのインスタンスがないため static である必要があります。そこにあるインスタンスメソッドは使わず、指すと一致しないメソッドとして診断されます。インスタンスのマッパーは構造体の中にも宣言できます。

```csharp
public sealed partial class InvoiceMapper
{
    private readonly ITaxService taxService;

    public InvoiceMapper(ITaxService taxService)
    {
        this.taxService = taxService;
    }

    [Mapper]
    [MapUsing(nameof(InvoiceDto.Tax), nameof(CalculateTax))]
    [MapProperty(nameof(InvoiceDto.Customer), Converter = nameof(Normalize))]
    public partial InvoiceDto Map(Invoice source);

    private decimal CalculateTax(Invoice source) => taxService.Calculate(source.Amount);  // インスタンスメソッド

    private static string Normalize(string value) => value.Trim();                       // static メソッド
}
```

static のマッパーが、マッパーのクラスとその基底クラスにインスタンスメソッドしかない名前を指すと、呼べないため、その名前を書いた属性の位置で診断されます（SMP0105）。マッパーをインスタンスメソッドにするか、メソッドを static にしてください。名前は C# と同じく、派生クラスのメソッドが同じシグネチャの基底クラスのメソッドを隠すものとして探すため、基底クラスの static メソッドを隠す、マッパーのクラスのインスタンスメソッドも同じように診断されます。拡張メソッドのマッパーは static のため、static メソッドだけを呼びます。

クラスのインスタンスのマッパーでは、`[MapExpression]` の式からもインスタンスのメンバーを使えます。構造体では、ローカル関数から `this` を使えないため（CS1673）使えず、`[MapUsing]` のインスタンスメソッドがその代わりになります（[`[MapExpression]`](#mapexpression)）。

### 値の受け取り方

`[MapUsing]` のメソッド、`[MapProperty]` の `Converter`、`[MapCondition]` のメソッドは、値（`[MapUsing]` はソース、ほかはソースのメンバー）をその型のまま受け取るほか、値渡しの引数なら、C# が渡すのと同じく暗黙に変換できる型としても受け取れます。基底クラスやインターフェイス（`Person` のソースに `private static string Label(IHasName x)`、`List<string>` のメンバーに `IEnumerable<string>` を受け取るコンバーター）、`object` や構造体が実装するインターフェイス（ボックス化。`DateTime` に `IFormattable`）、より広い数値（`int` に `long`）、null 許容の構造体（`int` に `int?`）、ユーザー定義の暗黙の変換（エラー扱いの `[Obsolete]` のものは除く）です。`in` の引数は、値の型そのものだけを受け取ります。

null 許容の構造体は、中の構造体（`int?` なら `int`）を受け取るコンバーターや条件、または値渡しで、中の値から暗黙に変換できる型（`long` や `double`、ユーザー定義の変換）を受け取るものに `Value` を渡します。null を受け付けない引数と同じく、値があるときだけ渡します（[Null 代替値](#null-代替値nullvalue)、[条件付きマッピング](#条件付きマッピング)）。中の構造体そのものを受け取るものが先で、次に null 許容の構造体を変換して受け取るもの（`long?` や `object`）、最後に中の値を変換して受け取るものです。ユーザー定義の変換を通して渡す値も、変換演算子の引数が null を受け付けなければ、値があるときだけ渡します。

基底クラスから派生クラスへのような、明示的な変換でしか値を受け取れないメソッドは一致せず、診断されます（SMP0110、SMP0112、SMP0201）。

[カスタムパラメーター](#カスタムパラメーター)は、メソッドが宣言したものを値の後に渡します。`[BeforeMap]` / `[AfterMap]` のコールバックはソースと destination を受け取り（[Before / After コールバック](#before--after-コールバック)）、`[MapNested]` / `[MapCollection]` のマッパーはソースのメンバーや要素を受け取ります（[子オブジェクト](#子オブジェクト)、[要素のマッパー](#要素のマッパー)）。どちらも、その後に宣言したカスタムパラメーターを受け取ります。

### オーバーロード

オーバーロードは、C# が呼び出しを結び付けるものを使います。値の型を受け取るものが先で、なければ引数の型がもっとも具体的なもの（基底クラスやインターフェイスより、それを継承したクラス。`int` には `object` より `long`）です。選んだメソッドに呼び出しが結び付かないときは、結び付く方を使います（値を受け取れる派生クラスのメソッドがあると、基底クラスのメソッドは候補から外れます。値渡しのものは `in` のものより先です）。あいまいな呼び出し（CS0121）や、一致しないメソッド（明示的な変換で値を受け取るもの、ジェネリックメソッド、省略可能な引数や `params` のあるもの、エラー扱いの `[Obsolete]` のもの）に結び付く、または結び付きうる呼び出しは、一致しないメソッドとして診断されます。結び付くメソッドが別の型を返すときは、戻り値の型で診断されます（SMP0111、SMP0202。条件が `bool` を返さないときは SMP0112）。カスタムパラメーターをもっとも多く受け取るオーバーロードが先で、同じ数の違うものを受け取るオーバーロードは、呼び出しがどれかを選べないため、これも一致しないメソッドとして診断されます（[カスタムパラメーター](#カスタムパラメーター)）。

### 戻り値

- `[MapUsing]` のメソッドは、ターゲットの型か、代入と同じく C# が暗黙に変換できる型を返します（そうでなければ SMP0202）。null 許容の参照や、`[return: MaybeNull]` で null になりうるとした参照を返し、ターゲットが null 許容でないときは `!` を付けて受け取ります。ただし、最初の引数を指定した `[return: NotNullIfNotNull]` で、マッパーの null 検査を通った source には null でない値を返すと分かるときは、そのまま受け取ります。
- `[MapProperty]` の `Converter` は、ターゲットの型か、`[MapUsing]` のメソッドと同じく暗黙に変換できる型を返します（そうでなければ SMP0111）。null 許容の参照や、`[return: MaybeNull]` で null になりうるとした参照を返し、ターゲットが null 許容でないときは `!` を付けて受け取ります。null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるときは、そのまま受け取ります。
- `[MapCondition]` のメソッドは `bool` を返します（そうでなければ SMP0112）。
- コンストラクタの引数では、ターゲットの型は引数の型です（[コンストラクタ引数の変換](#コンストラクタ引数の変換)）。

---

## プロパティパス

`[MapProperty]` のターゲットやソースにはドット付きパスを書け、ネストしたメンバーを展開・集約できます。`[MapConstant]`・`[MapExpression]`・`[MapUsing]` のターゲットも同じように途中のメンバーを通り、`[MapFrom]` のプロパティパスはソースのパスと同じように読みます。`[MapNested]` と `[MapCollection]` のソースはソースの型のプロパティで、ドット付きのパスにはできません（SMP0206）。

### Flatten

ネストしたソースからフラットな destination へ：

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

`Location.Lat` の `GeoPoint? Location` のように、パスの途中の null 許容の構造体は、同じ検査の下で中の構造体を通して読みます（`source.Location.Value.Lat`）。5 段以上のメンバー（null 許容の構造体の `Value` も 1 段と数えます）を通して読む値は C# が null の検査を追わないため、変換器・条件・変換に渡す前に検査するところで変数に受け（`if (source.Location.Value.In.Value.V is { } __value_V)`）、その変数を使います。パスの末端の値はメンバーと同じく変換します。enum は別の enum や文字列との間ではメンバーの名前で、数値との間ではキャストで変換し、メソッドのカルチャと書式も当てはまります。

パスは、マッパーのクラスから呼べる getter のあるプロパティを、継承したものも含めてたどります。そのようなプロパティでないものや、エラー扱いの `[Obsolete]` のものは診断されます（SMP0108）。途中の `[MaybeNull]` のメンバーも null になりえます（[`[MaybeNull]` のメンバー](#maybenull-のメンバー)）。

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

そのまま残せないコンストラクタの引数やオブジェクト初期化子の項目には、`NullValue`、null を受け取るターゲットには `null`、それ以外には `default` を渡します（[コンストラクタ引数の変換](#コンストラクタ引数の変換)）。

### Unflatten

フラットなソースからネストした destination へ：

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

マッパーから代入できない中間のメンバー（get だけ、`init` 専用、マッパーから呼べないセッター）や、マッパーが作れない型（抽象クラス、インターフェイス、引数なしで呼べるコンストラクタがない型、required メンバーのある型）のメンバーはインスタンス化せず、持っているインスタンスを埋めます。null のときは何も代入しません：

```csharp
// Destination.Child: public DestinationChild Child { get; } = new();
if (destination.Child is not null)
{
    destination.Child.Value = source.Value1;
}
```

struct のプロパティは値なので、ローカルに写して埋め、書き戻します。struct のフィールドはそのまま埋めます。書き戻せないもの（get だけのプロパティ、`readonly` フィールド）は診断されます（SMP0102）：

```csharp
// Destination.Point: public Point Point { get; set; } (a struct)
{
    var __copy0 = destination.Point;
    __copy0.X = source.Value1;
    destination.Point = __copy0;
}
```

ドット付きのターゲットは、宛先のメンバー `GeoPoint? Location` への `Location.Lat` のように、null 許容の構造体を通れません。中の構造体は `Value` から写しとして読むため、書き込んでも戻すセッターがないからです。このようなパスは、そのことを示す文言で診断されます（SMP0102）。メンバー全体を `[MapUsing]` などで写してください。ドット付きのソースは null 許容の構造体を通して読めます（[Flatten](#flatten) を参照）。途中のメンバーの getter をマッパーから呼べないパスも診断されます（SMP0102）。

パスの末尾の `init` 専用メンバーは、オブジェクト初期化子でしか設定できません。戻り値のあるマッパーは、通る途中のメンバーを作りながら初期化子で設定します。途中のメンバーは、初期化子で代入でき、作れる必要があります。void マッパーでは設定できず（SMP0302）、初期化子でも作れないパス（get だけのメンバーを通るものなど）は診断されます（SMP0102）：

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

作る途中のメンバーは、型引数の null 許容注釈を保ちます（[作るインスタンスの null 許容注釈](#作るインスタンスの-null-許容注釈)）。

### ドット付きパスと自動マッピング

メンバーの中へのドット付きパスは、`[MapProperty]`・`[MapConstant]`・`[MapExpression]`・`[MapUsing]` のどれでも、そのメンバーの自動マッピングに代わり、メンバー全体は自動ではマッピングされません。パスは destination が持つメンバーか作ったメンバーに書き込み、ソースのオブジェクトには書き込みません。`[MapIgnore]` で除外したメンバーでも、その中へのドット付きパスはマッピングされます。属性でメンバー全体をマッピングしたうえで、その中へドット付きパスを書くことはできません（SMP0101）。戻り値のあるマッパーが呼ぶコンストラクタが引数から代入するメンバー（位置指定 `record` の引数など）の中へも書けません。パスがコンストラクタに渡したオブジェクトに書き込むことになるためです（SMP0301）。戻り値のあるマッパーが設定する `required` メンバーは、型を作れればオブジェクト初期化子で作り、パスは構築後にその中へ書き込みます（末尾が `init` 専用のメンバーなら初期化子の中で書きます）。

---

## 子オブジェクト

`[MapNested]` は、マッパーメソッド（普通はほかの `[Mapper]` メソッド）でメンバーをマッピングします。

```csharp
[Mapper]
[MapNested(nameof(Destination.Child), nameof(Source.Child), Mapper = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

生成コード：

```csharp
destination.Child = source.Child is not null ? MapChild(source.Child!) : default!;
```

- マッパーは `Mapper` で指定する必要があります（ないときは SMP0214。メッセージで指定がないことを示します）。[属性が指すメソッド](#属性が指すメソッド)と同じように探すため、インスタンスのマッパーではインスタンスメソッドでもよく、ソース（と void のマッパーならインスタンス）の後に宣言した[カスタムパラメーター](#カスタムパラメーター)を受け取ります。`CultureInfo` の引数のカルチャも、こうして子オブジェクトの変換に渡ります。
- ソースはソースの型のプロパティで、ドット付きのパスにはできず、省略時はターゲット名です。見つからないソース、マッパーから呼べる getter がないもの、エラー扱いの `[Obsolete]` のものは診断され（SMP0206）、見つからないターゲットも診断されます（SMP0207）。マッパーから呼べるセッターも `init` アクセサーもないターゲットや、void マッパーでの `init` 専用のターゲットも診断されます（SMP0209）。
- `DestinationChild MapChild(SourceChild? source)` のように引数が null を受け付けるマッパーには、null の source のメンバーも渡し、ターゲットが受け取るものはマッパーが決めます（`destination.Child = MapChild(source.Child);`）。void のマッパーは、ターゲットのために作ったインスタンスを埋めます。引数が null を受け付けないマッパーは値があるときだけ呼び、source が null のときは上のとおりターゲットに `default` を入れます。null 許容の注釈なしで宣言した、または `[MaybeNull]` の付いた source のメンバーも null になりえます（[Null 処理](#null-処理)）。
- null 許容の参照を返すマッパーの結果は、null 許容でないターゲットには `!` を付けて入れます。ただし、マッパーに null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるとき（生成したマッパーはこれを宣言します）はそのまま入れます。null 許容でない source と `DestinationChild? MapChild(SourceChild? source)` なら `destination.Child = MapChild(source.Child);` です。
- void のマッパーは、生成コードが `new()` で作り、ターゲットの型引数の null 許容注釈を保ったインスタンス（`new Box<string?>()`）を埋めます。そのため、ターゲットの型がそれを許すとき、つまり構造体か、引数なしで呼べるコンストラクタがあってそのコンストラクタが設定しない required メンバーのない、abstract でないクラスのときだけ一致します。
- マッパーは、source のメンバーをその型のまま、または暗黙の参照変換で変換できる型（基底クラスやインターフェイス。値渡しのとき）として受け取り、ターゲットの型、同じように変換できる型（実装するインターフェイスの型のターゲットに対するクラスなど）、または null 許容の構造体のターゲットに対するその構造体を返します。null 許容の構造体のメンバーは、構造体を受け取るマッパーに、null を調べた後で中の値として渡し、null のときは参照型が null のときと同じく `default` を入れます（`source.Point is not null ? MapPoint(source.Point.Value) : default!`）。void のマッパーは、ターゲットのために作ったインスタンスを、その型のまま、または値渡しなら変換できる型として受け取ります。このように変換できないマッパーは一致しません（SMP0214）。
- オーバーロードは、C# が呼び出しを結び付けるものを使います。型そのものを受け取るものが、変換を通すものより先です。一致しないメソッド（別の型を返す、より具体的なもの、ジェネリックメソッド、省略可能な引数や `params` のあるもの、エラー扱いの `[Obsolete]` のもの）に結び付く呼び出しや、同じように変換を通して一致する 2 つのオーバーロード（呼び出しがあいまいになります）は診断されます（SMP0214）。
- 戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、引数の型の値を構築の前に作り、引数として渡します。`init` 専用のターゲットや、呼ぶコンストラクタが設定しない `required` のターゲットには、戻り値のあるマッパーが `[MapCollection]` と同じく構築の前に値を作り、オブジェクト初期化子で入れます（[`init` 専用と `required` のメンバー](#init-専用と-required-のメンバー)）。
- `[MapNested]` のターゲットはマッパーが代入するため、そこへの `[MapCondition]` は診断されます（SMP0109）。

---

## コレクション

### 要素の写し方

`[MapCollection]` は、要素マッパーメソッドでコレクションのメンバーを要素ごとにマッピングします。要素マッパーは `Mapper` で指定する必要があります（ないときは SMP0213。メッセージで指定がないことを示します）。ソースはソースの型のプロパティで、ドット付きのパスにはできず（SMP0206）、省略時はターゲット名です。

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

ループはソースとターゲットのコレクション型に合わせてインラインで生成されます。ソースは `IEnumerable<T>` を実装する型、`Memory<T>`、`ReadOnlyMemory<T>` で（そうでなければ SMP0210）、ターゲットは `IEnumerable<T>` を実装する型です（そうでなければ SMP0211）。見つからないソースやターゲットは診断され（SMP0206、SMP0207）、マッパーから呼べるセッターも `init` アクセサーもないターゲットや、void マッパーでの `init` 専用のターゲットも診断されます（SMP0209。`InPlace` は持っているインスタンスを詰め直します）。

ソースコレクションが null の場合はターゲットに `default` を代入します。null 許容、null 許容の注釈なし、または `[MaybeNull]` で宣言したソースコレクションも、同じように null を調べます。

戻り値のあるマッパーのコンストラクタが代入するメンバーや、同じ名前のメンバーがない引数を指すターゲットでは、引数の型のコレクションを構築の前に作り、引数として渡します。`init` 専用のメンバーや、呼ぶコンストラクタが設定しない `required` のメンバーには、構築の前に作ってオブジェクト初期化子で入れます（[`init` 専用と `required` のメンバー](#init-専用と-required-のメンバー)）。`[MapCollection]` のターゲットはループが代入するため、そこへの `[MapCondition]` は診断されます（SMP0109）。

### ターゲットのコレクション

ターゲットには、ループが作るコレクションを代入します。

- `List<T>` とそのインターフェイスには `List<T>`
- 配列には配列
- 集合には `HashSet<T>`
- `IDictionary<TKey, TValue>` と `IReadOnlyDictionary<TKey, TValue>` には `Dictionary<TKey, TValue>`（要素マッパーが返す `KeyValuePair<TKey, TValue>` の組で埋めます）
- イミュータブル・フローズンなコレクションにはその型。作れるのは `ImmutableArray<T>`・`ImmutableList<T>`・`ImmutableHashSet<T>` とそれらのインターフェイス、`FrozenSet<T>` で、`ImmutableDictionary<TKey, TValue>` や `FrozenDictionary<TKey, TValue>` などほかのものは、コレクション変換器で作る場合を除き診断されます（SMP0212）
- `ObservableCollection<T>` や `class ItemList : List<Item>` のような、マッパーが作れるコレクションクラスは、その型のコンストラクタで作って `ICollection<T>` として詰めます。作れないものは、コレクション変換器で作る場合を除き診断されます（SMP0212）

作ったコレクションを受け取れないターゲットは診断されます（SMP0212）。作るコレクションはターゲットの要素の null 許容注釈を保ちます（`List<DestinationChild?>`）。

### 要素のマッパー

- 要素マッパーは[属性が指すメソッド](#属性が指すメソッド)と同じように探すため、インスタンスのマッパーではインスタンスメソッドでもよく、要素（と void のマッパーならインスタンス）の後に宣言した[カスタムパラメーター](#カスタムパラメーター)を受け取ります。ただし、コレクション変換器にデリゲートとして渡すときは受け取れないため、一致しません（SMP0213）。要素の型は `[MapNested]` のマッパーと同じ規則で照合し（[子オブジェクト](#子オブジェクト)）、一致しないメソッドに結び付く呼び出しは診断されます（SMP0213）。
- void の要素マッパー `(SourceChild, DestinationChild)` は `new DestinationChild()` で作ったインスタンスを埋めるため、要素の型は `new()` で作れる必要があります（そうでなければ SMP0213）。
- `DestinationChild? MapChild(SourceChild? source)` のように null を受け取るマッパー（null を返すのは source が null のときだけです）の結果は、null 許容でない要素には `[MapNested]` のターゲットと同じく `!` を付けて受け取ります。ただし、要素が null でなく、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるとき（生成したマッパーはこれを宣言します）は、そのまま受け取ります（`List<SourceChild>` のソースなら `__dst[__i] = MapChild(__src[__i]);`）。
- null 許容の構造体の要素は、構造体を受け取るマッパーに中の値として渡し、null の要素は `default` になります。
- null になりうる参照の要素（null 許容のもの、または null 許容の注釈なしで宣言したもの）も、引数が null を受け付けないマッパーには値があるときだけ同じように渡し、null の要素は `default` になります（`__src[__i] is { } __value ? MapChild(__value) : default!`）。
- マッパーをデリゲートとして受け取るコレクション変換器には、すべての要素が渡ります。そのときマッパーの引数は、デリゲートの引数と合う必要があります（`Func` / `Action` なら値渡し）。`ref struct` のインスタンスメソッドは、インスタンスをボックス化することになるためデリゲートにできません。そのため、`ref struct` のマッパーでは、インスタンスメソッドの要素マッパーはコレクション変換器と一致せず（SMP0213）、ループが呼びます。

### 独自のコレクションクラス

独自のコレクションクラスは、基底の型やインターフェイスで実装している `IEnumerable<T>` によって、ソースでもターゲットでもコレクションとして扱います。

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

### コレクションそのもののマッピング

マッパーが写すのはオブジェクトで、コレクションではありません。source や destination が、フレームワークのコレクション（リスト・集合・辞書とそれらのインターフェイス、`PriorityQueue<TElement, TPriority>`、イミュータブル・フローズン・コンカレント・ObjectModel のもの）、それを継承したクラス（`class ItemList : List<Item>`）、配列、配列やメモリーの要素のビュー（`ArraySegment<T>`・`Memory<T>`・`ReadOnlyMemory<T>`・`Span<T>`・`ReadOnlySpan<T>`）、タプル、それらに制約された型引数（`T Create<T>(Item source) where T : List<ItemDto>, new()`）のマッパーは診断されます（SMP0007）。コレクションのメンバー（`Count`・`Capacity`）を写すだけで、要素をひとつも写さないためです。要素は要素の型のマッパーで写すか、コレクションを持つ型を `[MapCollection]` で写してください：

```csharp
[Mapper]
public static partial ItemDto ToDto(Item source);

// [Mapper] List<ItemDto> ToDtos(List<Item> source) は診断される
var dtos = items.Select(ToDto).ToList();
```

`IEnumerable<T>` を実装するだけの独自の型（件数を持つページなど）や、`IReadOnlyList<T>` を継承する独自のインターフェイスは、ほかの型と同じくメンバーで写します。

### コレクション変換器

コレクション変換器（マッパーメソッドかそのクラスに付けた `[CollectionConverter]`）を指定すると、ループの代わりにそのメソッドが呼ばれます（例: `CustomCollectionConverter.ToList<SourceChild, DestinationChild>(source.Children, MapChild)!`）。

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

呼ぶメソッドは、`[MapCollection]` の `Converter` で指定しなければターゲットの型で決まり（`ToList`・`ToArray`・`ToHashSet`・`ToImmutableArray` など）、`Method<TSourceElement, TTargetElement>(source, mapper)` として呼ばれます。`Converter` は `[CollectionConverter]` の型のメソッドを指し、指定がなければ `DefaultCollectionConverter` のメソッドを指します。メソッドがない場合、ソースのコレクションを受け取れない場合、型パラメーターの制約を満たさない場合、ターゲットのプロパティが受け取れない型を返す場合は診断されます（SMP0110）。変換器のクラスのエラー扱いの `[Obsolete]` のメソッドは呼ばず、一致しないものとして診断されます（SMP0110）。警告扱いのものは呼びます。要素マッパーはデリゲートとしてメソッドに渡すため、カスタムパラメーターを受け取れず、デリゲートがボックス化することになる `ref struct` のインスタンスメソッドにもできません。そのような要素マッパーは、コレクション変換器とは一致しません（SMP0213。[要素のマッパー](#要素のマッパー)）。

`DefaultCollectionConverter` は、関数のマッパー（`Func<TSource, TDest>`）と、`new TDest()` を埋める void のアクションのマッパー（`Action<TSource, TDest>`）のどちらにも対応した、次のメソッドを提供します。

| メソッド | 結果 | ソースのオーバーロード（`Func` のマッパー） |
|---------|------|------------------------------------------|
| `ToArray` | `TDest[]?` | `IEnumerable<T>`・`T[]`・`List<T>`・`ReadOnlySpan<T>`・`IReadOnlyCollection<T>` |
| `ToList` | `List<TDest>?` | 同上 |
| `ToHashSet` | `HashSet<TDest>?` | 同上 |
| `ToImmutableArray` | `ImmutableArray<TDest>` | 同上 |
| `ToImmutableList` | `ImmutableList<TDest>?` | 同上 |
| `ToImmutableHashSet` | `ImmutableHashSet<TDest>?` | 同上 |
| `ToFrozenSet` | `FrozenSet<TDest>?` | 同上 |

ソースが null なら `null` を、`ImmutableArray<TDest>` では空の配列を返します。`Action` のオーバーロードは `IEnumerable<T>` のソースを受け取り、`new()` の制約が必要で、`RequiresUnreferencedCode` と `RequiresDynamicCode` が付いています（[NativeAOT / トリミング](#nativeaot--トリミング)）。

`CollectionStrategy.InPlace` は常にループを生成するため、コレクション変換器は使いません。

### 既存のコレクションに詰め直す

既定（`CollectionStrategy.Replace`）ではターゲットに新しいコレクションを代入します。`CollectionStrategy.InPlace` はターゲットのインスタンスを残したまま空にし、写した要素を追加するため、データバインディングのように、ほかから参照されているインスタンスを保てます。

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

ターゲットは `ICollection<T>` として空にして詰め直すため、宣言の型はそれを実装し、設計上読み取り専用でないものである必要があります。`IReadOnlyList<T>`・`IReadOnlyCollection<T>`・`IEnumerable<T>`・配列・イミュータブルやフローズンなコレクション・`ReadOnlyCollection<T>` は診断されます（SMP0208）。ターゲットが null のときは次のとおりです。

- マッパーから代入できるプロパティには、その型の新しいインスタンス（例: `new ObservableCollection<T>()`）を作ります。インターフェイスには `List<T>`（`ISet<T>` には `HashSet<T>`、`IDictionary<TKey, TValue>` には `Dictionary<TKey, TValue>`）を作ります。どちらにも当たらない型は診断されます（SMP0212）。
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

`IList<T>` などで宣言したターゲットに、実行時に読み取り専用のインスタンス（配列など）が入っていると、`Clear` で `NotSupportedException` になります。`InPlace` の約束どおりインスタンスはそのまま保ち、呼び出し側の知らないうちに置き換えることはしません。`InPlace` は常にループを生成し、コレクション変換器は使いません。元のコレクションが null のときは、ターゲットを空にも置き換えもせず、そのまま残します。

戻り値のあるマッパーのコンストラクタが引数から代入するメンバーと、戻り値のあるマッパーが作る destination の `required` メンバーは、構築の前に設定する必要があり、詰め直すインスタンスがないため診断されます（SMP0208。`required` メンバーは、呼ぶコンストラクタに `[SetsRequiredMembers]` があれば対象外です）。`init` 専用のメンバーは、get だけのものと同じく、持っているインスタンスを詰め直します。

---

## コンストラクタと record

### コンストラクタの呼び出し

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

戻り値のあるマッパーが呼ぶコンストラクタと、引数を渡すかどうかは、[コンストラクタの選び方](#コンストラクタの選び方)のとおりに決めます。

### void マッパーとコンストラクタ

void マッパーは構築を行わないため、destination のコンストラクタは影響しません。コンストラクタでしか代入されないメンバーは、get だけのプロパティと同じく自動マッピングの対象から外れます。

> `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティなど。ドット付きパスの末尾のものや、`init` 専用の構造体のプロパティを通るものも）にも、`[MapProperty]` で指定したコンストラクタでしか代入されないメンバーにも代入できません（SMP0302）。

### コンストラクタの引数への値

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

コンストラクタが代入するメンバーには、`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` の値も使えます。値は引数に渡します（`new Dst(Build(src))`）。`[MapNested]` と `[MapCollection]` は構築の前に値を作ります。`InPlace` は構築の前に詰め直すインスタンスがないため使えません（SMP0208）。

コンストラクタが代入するメンバーの中へのドット付きのターゲット（`record Dst(Child Child)` への `[MapProperty("Child.Value", ...)]` など）は診断されます（SMP0301）。コンストラクタに渡したオブジェクトに書き込むことになり、引数がソースのものをそのまま渡すときはソースのオブジェクトを書き換えるためです。

### `init` 専用と `required` のメンバー

`[MapNested]` と `[MapCollection]` は、`init` 専用のメンバーと、呼ぶコンストラクタが設定しない `required` のメンバーの値を構築の前に作り、オブジェクト初期化子で入れます。null の扱い、要素の注釈、マッパーの照合はほかのターゲットと同じです。void マッパーは `init` 専用のメンバーに代入できません（SMP0209）。自動マッピングとほかの属性も、戻り値のあるマッパーでは `init` 専用のメンバーをオブジェクト初期化子で代入します。

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

destination の `required` メンバー（プロパティとフィールド。アクセシビリティを問わず、基底クラスのものも含む）は、戻り値のあるマッパーがオブジェクト初期化子で設定するため、どれもマッピングが必要で（SMP0308）、除外できません（SMP0304）。自動マッピングと `[MapProperty]` は public のプロパティを対象にし、`[MapConstant]`・`[MapExpression]`・`[MapUsing]` は `internal` のメンバーにも使えます。ドット付きパスが中へ書き込む `required` メンバーは、型を作れればオブジェクト初期化子で作り、作れないとき（abstract、引数なしで呼べるコンストラクタがない、それ自身の required メンバーがある）だけ診断されます（SMP0308）。呼ぶコンストラクタが引数から代入する `required` メンバーは診断されます（SMP0307）。オブジェクト初期化子で設定し直すことになり、コンストラクタが引数から作ったものを上書きするため、コンストラクタに `[SetsRequiredMembers]` が必要です。呼ぶコンストラクタに `[SetsRequiredMembers]` があれば必須ではなく、マッピングしないものはコンストラクタが設定した値のまま、マッピングするものは今までどおりオブジェクト初期化子で設定し、`[MapNested]` / `[MapCollection]` も構築後に代入できます。void マッパーはすでにあるインスタンスを埋めるので、関係しません。

### コンストラクタの選び方

戻り値のあるマッパーが呼ぶコンストラクタは、次の規則で選びます。

- 候補は、引数を持つと宣言されたコンストラクタのうち、マッパーのクラスから呼べて、どの引数も値で受け取れるものです。呼べない `private` や `protected` のもの、エラーの `[Obsolete]` のもの、`ref`・`out`・`ref readonly` の引数を持つものは候補にしません（`in` の引数は値で受け取れます）。abstract のクラスには候補がありません。
- コンストラクタでしか受け取れない宛先（同じ名前のメンバーがない引数名や、マッパーのクラスから setter を呼べないメンバー）に属性が値を与えるときは、それでコンストラクタを選びます。マッピングがどの引数にも値を持つ候補のうち、その宛先を最も多く受け取るもの、次に警告扱いの `[Obsolete]` でないもの、次に最長のもの、次に先に宣言したものを呼びます。宛先を受け取れるのが警告扱いの `[Obsolete]` のコンストラクタだけなら、それを呼びます（CS0618 の警告が出ます）。setter で受け取れる宛先はコンストラクタを選ぶ理由にしないため、setter をすべて呼べる型の構築は変わりません。そのような候補のどれも受け取れない宛先は診断されます（SMP0102）。
- それ以外では、警告扱いの `[Obsolete]`（CS0618）のコンストラクタは、ほかに destination を作る方法がないときだけ呼びます。マッピングがどの引数にも値を持つほかの候補か、引数なしで呼べるコンストラクタがあれば選ばず、警告扱いの `[Obsolete]` の引数なしのコンストラクタも、引数なしで作る方法として数えません。
- それ以外は、構築に引数を使うかを最長の候補で決めます。型が `record` である、構築後にマッパーが代入できる対応プロパティを持たない引数がある（プロパティがない、get だけ、`init` 専用、`private set` のようにマッパーのクラスから setter を呼べない）、または public なパラメータレスコンストラクタがない場合に使います。それ以外は `new Dst()` + プロパティ代入（init 専用メンバーはオブジェクト初期化子で代入）を生成します。
- 呼ぶのは、マッピングがどの引数にも値を持つ候補のうち最長のものです。値は、引数か、引数が代入するメンバーに対応するソースのプロパティ、またはどちらかを指す属性です。`[MapIgnore]` がメンバー（または引数そのもの）を指す引数には値がありません。値のない省略可能な引数（既定値、`[Optional]`、`params`）は渡さずに既定値に任せ、その後ろの引数は名前付きで渡します（`new Dst(src.A, c: src.C)`）。省いた呼び出しをほかのコンストラクタも受け取れる（同じ型で、残りの引数が省略可能）候補は、呼び出しがそちらに結び付くか、あいまいになるため選びません。その候補も引数を必要としない（どの引数もマッパーが代入できるプロパティに対応し、public なパラメータレスコンストラクタがある）ときは、`new Dst()` を生成します。
- どの候補にも値がそろわないときは、引数なしで作れる destination なら `new Dst()` で作り、コンストラクタでしか代入されないメンバーは写しません。引数なしで作れないものは最長の候補を使い、値のない引数を診断します（SMP0305。`[MapIgnore]` が指すものは SMP0304）。
- 作れない destination（abstract のクラス、インターフェイス、候補も引数なしで呼べるコンストラクタもない型、`new()` と `struct` のどちらの制約もない型パラメーター）は診断されます（SMP0303）。

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

コンストラクタ引数は通常のプロパティ代入と同じ変換パイプラインを通るため、型変換・`Converter`・`NullValue`・カルチャと書式の指定がすべて適用されます。オブジェクト初期化子で代入される `init` 専用メンバーも同様です。引数の値は、代入先のメンバーの型ではなく、引数の型に対して確かめて変換します。`int` のプロパティを代入する `string` の引数には、`string` のプロパティと同じく文字列への変換を使います。`Converter`・`NullValue`・属性の値も、引数の型のプロパティと同じ規則で確かめます。メンバーの型を返すコンバーターやメソッドは、引数の型が違えば、その型のプロパティに対するときと同じく診断されます（SMP0111・SMP0202・SMP0205・SMP0214・SMP0216）。

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

文ベースのオプションは、コンストラクタ引数やオブジェクト初期化子の項目には適用できません。`[MapCondition]` はメンバーを未代入のまま残す手段がなく、`NullBehavior.Skip` は保持すべき既存値がないためです。いずれも SMP0306 で拒否されます。

---

## Null 処理

### null 許容の値

| ソース型 | デスティネーション型 | 動作 |
|----------|---------------------|------|
| `T?` | `T?` | そのままコピー（null も含む） |
| `T?` | `T`（末端） | null の場合 `default!` を代入 |
| `T` | `T?` | そのままコピー |
| `T` | `T` | そのままコピー |

**source 側**の nullable 中間パスには、`[MapProperty]` でも `[MapFrom]` でも `if (... is not null)` ガードが付き、null 許容の構造体は中の構造体を通して読みます（`source.Location.Value.Lat`）。途中が null のときは、`NullValue` を指定したマッピングはその値を入れ、ほかはターゲットをそのまま残します（[Flatten](#flatten)）。
**destination 側**の nullable 中間パスは、マッパーから代入でき、作れれば `??= new` で自動インスタンス化され、そうでなければ持っているインスタンスを埋めます。ドット付きのターゲットは null 許容の構造体を通れません（SMP0102）（[Unflatten](#unflatten)）。

### Null 代替値（`NullValue`）

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullValue = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullValue = 0)]
public static partial void Map(Source source, Destination destination);
```

値は `[MapConstant]` の値と同じように書かれ、`source.Count ?? 0` のようにターゲットの型に変換できる必要があります（そうでなければ SMP0216。生成コードから参照できない値は SMP0215）。`NullValue = null` には null を受け取れるターゲットが必要です。`[Obsolete]` の enum メンバーは、同じ値の廃止されていないメンバーがあればその名前で書きます（[廃止されたメンバー](#廃止されたメンバー)）。

source をそのまま受け取る `Converter` と組み合わせたときは、source が null なら `NullValue` を入れ、値があるときだけ変換メソッドを呼びます（`source.Count is not null ? ToText(source.Count) : "none"`）。ドット付きのソースで途中のメンバーが null のときも `NullValue` を入れます（[Flatten](#flatten)）。

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

source をそのまま受け取る `Converter` も、値があるときだけ呼びます。ドット付きのソースで途中のメンバーが null のときも、ターゲットをそのまま残します。

コンストラクタやオブジェクト初期化子で代入されるメンバーには残すべき値がないため、`NullBehavior.Skip` は指定できません（SMP0306）。

### null 許容の注釈なし

null 許容の注釈なし（`#nullable disable` や、注釈なしでビルドしたライブラリ）で宣言した参照型は null かどうかを何も言っていないため、その値は null になりえます。そのような型の元の引数、source のメンバー、source のコレクションの要素は、null 許容のものと同じく扱います。読み進める前や、引数が null を受け付けない変換器・条件・`[MapNested]` のマッパー・要素のマッパーに渡す前に null を調べ、`NullValue` と `NullBehavior.Skip` も当てはまります。Strict モードでは、これらを null になりうる値として警告しません（SMP0502）。

### `[MaybeNull]` のメンバー

参照型の source のメンバーに、プロパティか getter の戻り値の `[MaybeNull]` が付いていれば（`[MaybeNull] public string Name { get; set; }`）、C# の読み方と同じく null になりうるため、null 許容の型のものと同じく扱います。同じように null を調べ、`NullValue`・`NullBehavior.Skip`・`[MapCondition]` も当てはまり、Strict モードでは警告します（SMP0502）。パスが指すプロパティの属性を見るため、属性のない override は null でないものとして読みます。`[return: MaybeNull]` の付いた `[MapFrom]` のメソッドの値も、null になりうるものとして扱います。`[return: MaybeNull]` の付いた変換器・`[MapUsing]` のメソッド・`[MapNested]` / `[MapCollection]` のマッパーも同じで、その結果は null 許容のものと同じく、null 許容でないターゲットには `!` を付けて受け取り、Strict モードでは警告します（SMP0502。[戻り値](#戻り値)）。

### null 許容の引数と戻り値

元の引数（void マッパーでは宛先の引数も）を `Map(Src? source)` のように null 許容で宣言すると、写す前に検査します。null 許容の注釈なしで宣言した元の引数と void マッパーの宛先の引数も同じです。カスタムパラメーターはそのまま渡します。null のときは何も写さず、戻り値のあるマッパーは `default` を返し、void マッパーは宛先に触れずに戻ります。

`Dst? Map(Src? source)` や `Point? Map(Src? source)` のように、元の引数が null になりうる、null 許容の型を返すマッパーは、元の引数が null のときだけ null を返すため、実装に `[return: NotNullIfNotNull("source")]`（引数の名前で）を付けます。null でない引数を渡した呼び出し側は、null 許容の警告なしで結果を使えます。宣言の側に同じ属性を付けてもかまいません。マッパーのクラスから使える `NotNullIfNotNullAttribute` がコンパイルにないとき（その写しのない .NET Standard 2.0 や .NET Framework）は付けません。`Dst Map(Src? source)` のように、null 許容で宣言した source から null を受け付けない型を返すマッパーは、source が null のとき `default` を返し、Strict モードで警告されます（SMP0502）。

`Point? Map(Src source)` のように null 許容の構造体を戻り値の型にすると、その中の構造体として作って埋めます。`Map(Point? source)` のように元の引数を null 許容の構造体にすると、その中の構造体のメンバーを持たないため診断されます（SMP0006）。構造体そのものを受け取り、null は呼び出す前に調べてください。

### 作るインスタンスの null 許容注釈

生成コードが作るインスタンスは、宣言どおり型引数の null 許容注釈を保ちます。`Box<string?>` と宣言した destination は `new Box<string?>()` で作り、ドット付きのターゲットの途中のメンバー、オブジェクト初期化子で作る `required` のメンバー、`[MapCollection]` が作るコレクション、`[MapNested]` / `[MapCollection]` の void のマッパーが埋めるインスタンスも同じです。

---

## 型変換

### そのまま代入するもの

同型・暗黙的変換可能な代入はコンバーター不要で直接生成されます。数値の拡大変換、値からその null 許容型への変換、暗黙の参照変換（変性によるものを含む。`IReadOnlyList<Circle>` から `IReadOnlyList<Shape>`、`Circle[]` から `Shape[]` など）がこれに当たります。両側が同じ enum ならそのまま写すため、フラグの組み合わせのようにメンバーのない値も保ちます。

### スペシャライズドメソッド

変換が必要な場合は、値の変換器のその型のスペシャライズドメソッドを呼びます。名前は `ConvertTo{TargetType}`（[カスタム型変換器](#カスタム型変換器)では `{Method}To{TargetType}`）です。直接の呼び出しは JIT のインライン展開と相性がよく、実行時にリフレクションを使いません。

```csharp
// string -> int
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);

// int -> string
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);
```

null 許容の値はジェネレーター側で処理し、値があるときだけ変換器を呼びます：

```csharp
// int? -> string
destination.StringValue = source.NullableValue is not null
    ? DefaultValueConverter.ConvertToString(source.NullableValue.GetValueOrDefault())
    : default!;
```

カルチャや書式が当てはまるときは、代わりにカルチャと書式を受け取るオーバーロードを呼びます（[カルチャを受け取る変換器のオーバーロード](#カルチャを受け取る変換器のオーバーロード)）。

### そのほかの変換

スペシャライズドメソッドのない型は、その型の変換演算子、`Parse`（文字列から `IParsable<T>` を実装する型へ。`Parse(text, provider)` として）、`ToString(format, provider)`（文字列へ）で変換します。カルチャは当てはまるカルチャ、なければインバリアントカルチャです（[カルチャと書式](#カルチャと書式)）。`IParsable<T>` の `Parse` は書式を受け取らないため、当てはまる `DateTimeFormat` や `NumberFormat` は使いません（[書式](#書式)）。数値をより狭い数値型へ写すときは C# のキャストと同じくキャストし（`(int)source.LongValue`）、enum はメンバーの switch か、数値との間のキャストで変換します。

どれでも変換できないものは、AOT に対応しない汎用の `Convert<TSource, TDestination>` へのフォールバックになるため、`[ValueConverter]` のクラスが引き受けない限り診断されます（SMP0402。[カスタム型変換器](#カスタム型変換器)）。`[ValueConverter]` のクラスは、スペシャライズドメソッドのない変換を、型の変換演算子・`Parse`・`ToString(format, provider)` ではなく、汎用のメソッドで変換します。ソースとターゲットがクラス・構造体・コレクションのとき（多くは属性なしでマッピングしようとした子のメンバーやコレクション）は、`[MapNested]` や `[MapCollection]` を使うようメッセージで案内します。

参照型のユーザー定義の変換は null 許容に持ち上げられないため、null になりうる値は値があるときだけ演算子を通します（`implicit operator string(Email email)` のある `Email?` なら `source.Email is not null ? (string)source.Email : null`）。

変換はエラー扱いの `[Obsolete]` のメンバーを呼ばず、ほかの変換があればそれを使います（[廃止されたメンバー](#廃止されたメンバー)）。

### enum の変換

別の enum へ写すときは、メンバーを名前で対応させます。フラグの組み合わせのように、ターゲットに同じ名前のメンバーがない値は `default` になり、null 許容の enum のターゲットでは `null` になります。文字列から enum へ写すときも同じく名前で対応させ、同じ名前のメンバーがない文字列は `Enum.Parse` に渡します（解析できなければ例外になります）。null 許容の enum のターゲットには `Enum.TryParse` で解析し、解析できなければ `null` にします（数字の文字列はその値になります）。enum から文字列へはメンバー名（メンバーのない値は `ToString()` の結果）で、数値との間はキャストで変換します。Strict モードでは、ターゲットの enum に同じ名前のメンバーがないソースの enum のメンバーを警告します（SMP0503）。

`[Obsolete]` の enum メンバーは、これらの switch では値のキャストで書きます（[廃止されたメンバー](#廃止されたメンバー)）。

### 既定の書式

カルチャも書式も当てはまらないとき（`DefaultCulture = Invariant` で、カルチャ名も `CultureInfo` の引数もないとき、または `Culture = ""` のとき）、値は次のように文字列との間で変換します（`DefaultValueConverter`、インバリアントカルチャ）：

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

ラウンドトリップ書式は、`DateTime` の秒の端数と種類（UTC なら `Z`、ローカルならオフセット）を保ちます。カルチャ（カルチャ名、`DefaultCulture = Current`、`CultureInfo` の引数）が当てはまるときは、そのカルチャの書式（`ToString(culture)`・`Parse(text, culture)`）になります。書式がなければ、`DateTime`・`DateTimeOffset`・`DateOnly`・`TimeOnly` はそのカルチャの一般の書式で書き、`TimeSpan` はカルチャによらない固定の書式 `c` で書きます。`DateTimeFormat` / `NumberFormat` で書式を指定できます（[カルチャと書式](#カルチャと書式)）。

### `DefaultValueConverter`

`DefaultValueConverter` は、`[ValueConverter]` を指定しないときに使う値の変換器です。スペシャライズドメソッドにはそれぞれ、カルチャを受け取らない `ConvertTo{TargetType}(value)` と、カルチャと書式を受け取る `ConvertTo{TargetType}(value, IFormatProvider culture, string? format)` の 2 つのオーバーロードがあります。

| 変換 | カルチャなし | カルチャと書式あり |
|------|-------------|-------------------|
| `string` から数値（`Int32`・`Int64`・`Int16`・`Byte`・`SByte`・`UInt32`・`UInt64`・`UInt16`・`Single`・`Double`・`Decimal`・`Half`・`Int128`・`UInt128`・`BigInteger`） | `Parse(text, InvariantCulture)` | `Parse(text, culture)`。数値の `Parse` は書式文字列を受け取らないため、書式は使わない |
| 数値から `string` | `ToString(InvariantCulture)` | `ToString(culture)`、または `ToString(format, culture)` |
| `string` から `bool`・`char`・`Guid`、それらから `string` | `Parse`、`ToString()` | 同じ（カルチャと書式は使わない） |
| `string` から `DateTime` | `DateTime.Parse(text, InvariantCulture, RoundtripKind)` | `DateTime.Parse(text, culture)`、または `DateTime.ParseExact(text, format, culture, ...)`（`O` / `o` なら `DateTimeStyles.RoundtripKind`） |
| `string` から `DateTimeOffset`・`DateOnly`・`TimeOnly`・`TimeSpan` | `Parse(text, InvariantCulture)` | `Parse(text, culture)`、または `ParseExact(text, format, culture)` |
| `DateTime`・`DateTimeOffset`・`DateOnly`・`TimeOnly` から `string` | `ToString("O", InvariantCulture)` | `ToString(culture)`、または `ToString(format, culture)` |
| `TimeSpan` から `string` | `ToString("c", InvariantCulture)` | `ToString(null, culture)`、または `ToString(format, culture)` |
| `int`・`long`・`float`・`double` から `Half` | キャスト | キャスト（カルチャと書式は使わない） |

`Char` は `IParsable<char>` / `ISpanParsable<char>` を明示的に実装しているため、呼べる public な `Char.Parse(ReadOnlySpan<char>, IFormatProvider)` がありません。スペシャライズドメソッドの `ConvertToChar` はその経路を避けます。

`DateTimeFormat = "O"`（`"o"` も）では、書式なしのときと同じく、文字列から `DateTime` へ `DateTimeStyles.RoundtripKind` 付きで読み、文字列の示す種類を保ちます（`DateTimeStyles.None` では `Z` がローカル時刻になります）。RFC 1123 の書式 `"R"`（`"r"`）では、`GMT` は書式の中の文字でタイムゾーンとしては読まないため、`DateTime.ParseExact` のとおり、書かれた時刻を種類が未指定のまま返します（`ToString("R")` も時刻を UTC に直さずに書きます）。

`Convert<TSource, TDestination>(value)` は汎用のフォールバックで、スペシャライズドメソッドがなく、変換が必要なとき（同じ型でも、値とその null 許容型の間でもないとき）に、生成コードが null 許容の値を処理した後で呼ばれます。ジェネレーターは `[ValueConverter]` を通して、そのクラスの `{Method}<TSource, TDestination>` としてだけ呼び、指定がなければ、フォールバックが必要な変換を診断します（SMP0402）。`DefaultValueConverter` のものは型で分岐し、JIT が型引数ごとに分岐を解決します。組み込みの数値の間はキャストで、`string` へはインバリアントカルチャで変換し、`string` から数値・`bool`・`DateTime`（ラウンドトリップの種類を保つ）・`Guid` へ解析し、`bool`・`DateTime`（`O`）・`Guid` を文字列に書きます。それ以外は（enum や互換な型のために）値を直接キャストし、ボックス化が起きます。

### カスタム型変換器

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

- スペシャライズドメソッドのない変換で使う汎用のフォールバックなど、変換器のメソッドが見つからない場合は診断されます（SMP0110）。
- カルチャ（カルチャ名、`DefaultCulture = Current`、`CultureInfo` の引数）か書式が当てはまるときは、使うスペシャライズドメソッドごとに `(value, IFormatProvider, string?)` のオーバーロードを用意する必要があります（そうでなければ SMP0110。[カルチャを受け取る変換器のオーバーロード](#カルチャを受け取る変換器のオーバーロード)）。
- そのオーバーロードの書式の引数には、書式が当てはまらなければ `null` が渡るため、`ConvertToString(decimal source, IFormatProvider culture, string? format)` のように `string?` で宣言してください。`string` で宣言すると、生成コードで null 許容の警告（CS8625）が出ます。
- 汎用のフォールバック `{Method}<TSource, TDestination>(TSource source)` は値だけを受け取ります。カルチャや書式が当てはまっていても、それらを受け取ることはありません。
- 変換器のクラスのメソッドは値を受け取り、カスタムパラメーターは受け取りません。
- 変換器のクラスのエラー扱いの `[Obsolete]` のメソッドは呼びません。ほかの変換があればそれを使い（スペシャライズドメソッドの代わりに汎用のメソッド）、なければ一致しないものとして診断されます（SMP0110）。警告扱いのものは呼びます。
- クラスの `[ValueConverter]` が与えた値についての診断は、その属性の位置で報告します。

### 変換器の優先順位

優先順位（高 → 低）：

| レベル | 適用範囲 |
|--------|----------|
| `[MapProperty(Converter = nameof(...))]` | 単一プロパティ |
| マッパーメソッドの `[ValueConverter]` | そのメソッドの全プロパティ |
| クラスの `[ValueConverter]` | クラス内の全マッパーメソッド |
| `DefaultValueConverter` | フォールバック |

`[MapProperty]` の `Converter` は、`[MapUsing]` のメソッドがソースを受け取るのと同じようにソースのメンバーを受け取ります。暗黙に変換できる型、null 許容の構造体なら中の構造体としても受け取れます（[値の受け取り方](#値の受け取り方)）。返す型は、ターゲットの型か暗黙に変換できる型です（[戻り値](#戻り値)）。

---

## カルチャと書式

値と文字列の間の変換はカルチャを使います。値の変換器のスペシャライズドメソッド、`IParsable<T>` を実装する型の `Parse`、ほかの型の `ToString(format, provider)` です。

```csharp
[MapperProfile(Culture = "ja-JP")]
internal static partial class AppMappers
{
    [Mapper(Culture = "de-DE", NumberFormat = "N2")]
    public static partial Dest Map(Src src);

    [Mapper]
    [MapProperty(nameof(Dest2.Amount), nameof(Src2.Price), Culture = "en-US", NumberFormat = "C")]
    public static partial Dest2 Map(Src2 src);

    [Mapper]
    public static partial Dest3 Map(Src3 src, CultureInfo culture);   // 呼び出し側がカルチャを渡す
}
```

### 既定のカルチャ

クラスかアセンブリの `[MapperProfile]` の `DefaultCulture` は、変換にカルチャ名が当てはまらず、メソッドに `CultureInfo` の引数もないときに使うカルチャです。

| `MapperCulture` | 変換 |
|-----------------|------|
| `Invariant`（既定） | 値の変換器の、カルチャに依存しない変換（カルチャを受け取らないオーバーロード）。数値はインバリアントカルチャ、日付と時刻は ISO 8601 のラウンドトリップ書式 `O`、`TimeSpan` は `c` です（[既定の書式](#既定の書式)） |
| `Current` | `CultureInfo.CurrentCulture`。変換のたびに読み、カルチャ名と同じく、変換器のカルチャを受け取るオーバーロードで変換します。数値はそのカルチャで、日付と時刻はその一般の書式（`ToString(culture)`。`TimeSpan` は `c`）です |

```csharp
// カルチャ名のない変換は、マッパーを動かすスレッドのカルチャを使う
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]
```

クラスのプロファイルの `DefaultCulture` がアセンブリのプロファイルのものより優先され、どちらも指定しなければ `Invariant` です。`[Mapper]` には `DefaultCulture` はありません。メソッドごとのカルチャは `Culture` か `CultureInfo` の引数で指定します。`Culture = ""` なら、`Current` の下でもインバリアントカルチャになります（[カルチャ名](#カルチャ名)）。

### カルチャ名

`[MapProperty]`・`[Mapper]`・`[MapperProfile]` の `Culture` は、`"ja-JP"` のようにカルチャを名前で指定します。英数字のサブタグをハイフンでつないだもの（`ja-JP`、`zh-Hant-TW` など。言語は 1〜8 文字の英字、ほかのサブタグは 1〜8 文字の英数字）で、アンダースコアの後に並べ替え順を付けられます（`de-DE_phoneb` など）。そうでなければ診断されます（SMP0401）。カルチャ名は、生成コードがカルチャを持つフィールドの名前になります。そのカルチャがあるかどうかはマッパーを動かすシステムによるため、確かめません。

空の名前（`Culture = ""`）は診断されません。`CultureInfo.InvariantCulture` の名前が `""` であるとおり、どのレベルでもインバリアントカルチャです。ほかのカルチャ名と同じく、そのレベルの優先順位で当てはまり（[カルチャの優先順位](#カルチャの優先順位)）、`[MapProperty(Culture = "")]` は `CultureInfo` の引数とメソッドのカルチャより、`[Mapper]` やプロファイルの `Culture = ""` は、それより後のプロファイルのカルチャと `DefaultCulture = MapperCulture.Current` より優先されます（現在のカルチャではなく、インバリアントカルチャになります）。その変換は、`Invariant` でカルチャがないときと同じく、変換器のカルチャを受け取らないオーバーロードで、書式が当てはまるときは `CultureInfo.InvariantCulture` で変換します。カルチャが `""` のメソッドの null 許容の `CultureInfo` の引数は、null ならインバリアントカルチャを使います。

`""` 以外のカルチャ名を指定すると、変換器のカルチャを受け取るオーバーロードで変換します。解決された `CultureInfo` は生成クラス内で `static readonly` フィールドとしてキャッシュされ、変換ごとの `GetCultureInfo(...)` 呼び出しコストを排除します。

### `CultureInfo` の引数

`System.Globalization.CultureInfo` 型のカスタムパラメーターは、そのメソッドの変換のカルチャを決め、変換器のカルチャを受け取るオーバーロードで変換します。

```csharp
internal static partial class OrderMappers
{
    [Mapper]
    public static partial OrderDto Map(Order source, CultureInfo culture);
}

var dto = OrderMappers.Map(order, CultureInfo.GetCultureInfo("fr-FR"));
```

これもカスタムパラメーターです。ほかのものと同じく、カスタムパラメーターを受け取る `[MapUsing]`・`[MapCondition]`・`[BeforeMap]`・`[AfterMap]` のメソッドと `[MapProperty]` の `Converter` に渡します（[カスタムパラメーター](#カスタムパラメーター)）。`CultureInfo` の引数を宣言した `[MapNested]` / `[MapCollection]` のマッパーにも渡すため、子オブジェクトや要素の変換にもそのカルチャが効きます。宣言していないマッパーは、自分で決めたカルチャで変換します。`CultureInfo` の引数が複数あるときは、変換器のカルチャの引数名と同じ `culture` という名前のものがメソッドの変換のカルチャになり、それぞれを名前でメソッドに渡します。複数あって `culture` という名前のものがなければ診断されます（SMP0008）。

null 許容の引数（`CultureInfo?`）や、null 許容の注釈なしで宣言した引数が実行時に null なら、その引数がないときにメソッドが使うカルチャを使います。カルチャ名のフィールド、`DefaultCulture = Current` なら現在のカルチャ、それ以外はインバリアントカルチャで、このときもカルチャを受け取るオーバーロードで変換します。インバリアントカルチャなら、日付はラウンドトリップ書式 `O` ではなく、インバリアントカルチャの一般の書式（`01/02/2024 03:04:05`）になります。

このような引数は、null を受け付けない引数（`CultureInfo` と宣言し、値渡しか `in` で受け取るもの）を持つ変換器・条件・`[MapUsing]` のメソッド・コールバック・`[MapNested]` / `[MapCollection]` のマッパーにも同じように渡ります。その引数には、マッパーの変換が使うカルチャ、つまり引数か、null ならメソッドのカルチャを渡します（`MapChild(source.Child, (culture ?? CultureInfo.InvariantCulture))` など）。そのため、`CultureInfo culture` と宣言した子のマッパーは、外側の変換と同じカルチャで変換し、null 許容の警告も出ません。`CultureInfo?` と宣言した引数には、引数をそのまま渡します。ほかの null になりうるカスタムパラメーター（ほかの型のものや、カルチャを決めるもの以外の `CultureInfo` の引数）はそのまま渡すため、null を受け付けない引数に渡すと、生成コードで null 許容の警告（CS8604）が出ます。

null を受け付けない `CultureInfo` の引数を持つメソッドの `[Mapper]` の `Culture` は、引数がカルチャを決めるため使われず、`[Mapper]` の位置で警告として診断されます（SMP0404）。null を受け付ける引数なら、`Culture` は null の引数が使うカルチャになるため、診断されません。プロファイルの `Culture` は既定値のため、引数が警告なしで代わり、`[MapProperty]` の `Culture` はそのマッピングに引き続き当てはまります。

### カルチャの優先順位

変換のカルチャは、次のうち最初に当てはまるものです。

1. `[MapProperty]` の `Culture`
2. メソッドの `CultureInfo` の引数
3. `[Mapper]` の `Culture`
4. クラスの `[MapperProfile]` の `Culture`
5. アセンブリの `[MapperProfile]` の `Culture`
6. クラスのプロファイル、次にアセンブリのプロファイルの `DefaultCulture`（どちらも指定しなければ `Invariant`）

1・3・4・5 のレベルの `Culture = ""` は、そのレベルでインバリアントカルチャを与えます（[カルチャ名](#カルチャ名)）。

### 書式

`[MapProperty]`・`[Mapper]`・`[MapperProfile]` の `DateTimeFormat` と `NumberFormat` は、値と文字列の間の変換の書式を指定します。それぞれ独立に、`[MapProperty]`、`[Mapper]`、クラスのプロファイル、アセンブリのプロファイルの順に決まり、カルチャとも独立です。`[MapperProfile(Culture = "ja-JP", NumberFormat = "N0")]` の下では、`[Mapper(NumberFormat = "N2")]` は ja-JP と N2、`[Mapper(Culture = "de-DE")]` は de-DE と N0 で書式化します。

書式にカルチャ名は要りません。カルチャ名がなければ、そのとき当てはまるカルチャ（`Invariant` ならインバリアントカルチャ、`Current` なら現在のカルチャ、または `CultureInfo` の引数）で書式を当てはめます。書式のある変換は、変換器のカルチャと書式を受け取るオーバーロードで変換します。

- `DateTimeFormat` は `DateTime`・`DateTimeOffset`・`DateOnly`・`TimeOnly`・`TimeSpan` と `string` の間の変換の書式で、`NumberFormat` はそれ以外の変換（数値と、ほかの型の `ToString(format, provider)`）の書式です。`DefaultValueConverter` は数値を `NumberFormat` で書き、文字列から数値へはカルチャだけで解析します。数値の `Parse` は書式を受け取らないためです。
- `IParsable<T>` を実装する型は、どの書式が当てはまっていても（`[MapProperty]`・`[Mapper]`・プロファイルのもの。アセンブリのプロファイルを含む）、その `Parse(text, provider)` で文字列から解析します（[そのほかの変換](#そのほかの変換)）。この `Parse` は書式を受け取らないため、当てはまるのはカルチャだけです。書式が渡るのは、組み込みの数値と日付・時刻の解析だけで、値の変換器の書式を受け取るオーバーロードに渡ります。そのスペシャライズドメソッドのない独自の `[ValueConverter]` は、文字列をその型の `Parse` ではなく、汎用のメソッドに渡します。
- `[Mapper]` とプロファイルの `DateTimeFormat` は、マッパーが変換する日付と時刻の型すべて（`DateTime`・`DateTimeOffset`・`DateOnly`・`TimeOnly`・`TimeSpan`）に使われます。型ごとに書式を分けるときは、`[MapProperty]` の `DateTimeFormat` で指定してください。`TimeSpan` の書式はほかの型と書き方が違い（`hh\:mm` のように区切りをエスケープします）、日付向けの書式は `TimeSpan` や `TimeOnly` では実行時に失敗します（`FormatException`）。
- 書式のある文字列から日付や時刻へは `ParseExact` で読みます。`DateTimeFormat = "O"`（`"o"` も）では、文字列から `DateTime` へも `DateTimeStyles.RoundtripKind` 付きで読み、文字列の示す種類を保ちます。`"R"`（`"r"`）では、`GMT` は書式の中の文字でタイムゾーンとしては読まないため、`DateTime.ParseExact` のとおり、書かれた時刻を種類が未指定のまま返します（`ToString("R")` も時刻を UTC に直さずに書きます）。

### カルチャを受け取る変換器のオーバーロード

カルチャ（カルチャ名、`DefaultCulture = Current`、`CultureInfo` の引数）か書式が当てはまるとき、変換器のスペシャライズドメソッドは、カルチャと書式を受け取るオーバーロード `(value, IFormatProvider, string?)` で呼ばれます（例: `DefaultValueConverter.ConvertToString(int source, IFormatProvider culture, string? format)`）。書式が当てはまらなければ、書式は `null` です。独自の `[ValueConverter]` は、そのとき使うスペシャライズドメソッドごとにこのオーバーロードを用意する必要があります（そうでなければ SMP0110）。カルチャの引数は `CultureInfo` を、その型か変換できる型（`IFormatProvider` など）で受け取り、書式の引数は `string` を受け取ります。書式が当てはまらなければ `null` が渡るため、書式の引数は `string?` で宣言します（`string` では生成コードで CS8625 が出ます）。独自の変換器の汎用のフォールバックは、カルチャと書式なしで呼ばれます（[カスタム型変換器](#カスタム型変換器)）。

---

## プロファイル

`[MapperProfile]` はマッパーメソッドの既定値を指定します。クラスや構造体に付ければ、その中で宣言したマッパーメソッドの既定値に、アセンブリに付ければ（`[assembly: MapperProfile(...)]`。プロジェクトのどのファイルでもかまいません）、アセンブリのすべてのマッパーメソッドの既定値になります。クラスのプロファイルは、その中の入れ子のクラスのマッパーメソッドには当てはまりません。入れ子のクラスのマッパーメソッドは、そのクラスのプロファイル、次にアセンブリのプロファイルを使います。

```csharp
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current, NameComparison = StringComparison.OrdinalIgnoreCase)]

[MapperProfile(Strict = true, NumberFormat = "N2")]
internal static partial class InvoiceMappers
{
    [Mapper]                                         // strict、N2、現在のカルチャ、大文字小文字を無視した名前
    public static partial InvoiceDto Map(Invoice source);

    [Mapper(Strict = false, NumberFormat = "N0")]    // strict でない、N0
    public static partial InvoiceSummary Summarize(Invoice source);
}
```

プロファイルの設定（`Strict`・`NameComparison`・`DefaultCulture`・`Culture`・`DateTimeFormat`・`NumberFormat`）はすべて既定値です。`[Mapper]` の設定がクラスのプロファイルより、クラスのプロファイルがアセンブリのプロファイルより優先され、設定ごとに独立に決まります。明示的に指定した設定が優先され、strict のプロファイルの下での `Strict = false` も同じです。`DefaultCulture` はプロファイルだけで指定します。`[MapProperty]` の `Culture`・`DateTimeFormat`・`NumberFormat` はそのマッピングについてこれらすべてより優先され、`CultureInfo` の引数は `[Mapper]` とプロファイルのカルチャより優先されます（[カルチャの優先順位](#カルチャの優先順位)）。

カルチャ名でない `Culture`（SMP0401）など、プロファイルが与えた値についての診断は、そのプロファイルの位置で報告します。アセンブリのプロファイルのカルチャ名でない `Culture` は、マッパーごとではなく、アセンブリの属性の位置で 1 回だけ報告し、マッパーはそのカルチャなしで変換します。

---

## コールバックと条件

### Before / After コールバック

```csharp
[Mapper]
[BeforeMap(nameof(BeforeMapping))]
[AfterMap(nameof(AfterMapping))]
public static partial void Map(Source source, Destination destination);

private static void BeforeMapping(Source source, Destination destination) { /* ... */ }
private static void AfterMapping(Source source, Destination destination) { /* ... */ }
```

- `[BeforeMap]` は代入の前に、`[AfterMap]` は代入の後にメソッドを呼びます（[代入の順序](#代入の順序)）。
- コールバックは[属性が指すメソッド](#属性が指すメソッド)と同じように探します。インスタンスのマッパーではインスタンスメソッドでもよく、ジェネリックメソッドは使いません。
- コールバックは、source と destination をその型のまま受け取るほか、値渡しの引数なら、変換できる基底クラスやインターフェイスとしても受け取れます。1 つのコールバックを複数のマッパーで使えます（`private static void Audit(IEntity source, IAuditable destination)`）。構造体の source や destination は、その型そのものだけに渡します。ボックス化すると写しに書き込むことになり、destination に反映されないためです。カスタムパラメーターは、宣言していればその後ろにその型のまま受け取ります。
- オーバーロードは C# が呼び出しを結び付けるものを使い、あいまいな呼び出しや、一致しないメソッドに結び付く呼び出しは診断されます（SMP0106 / SMP0107）。`(Source, Destination)` と、その後に宣言したカスタムパラメーターの形に一致しないコールバックも同じです。

### 条件付きマッピング

条件メソッドは source の値（とカスタムパラメーター）を受け取り、`true` を返したときだけ destination プロパティに代入されます。

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

- 条件が守るのは、ターゲットのプロパティマッピング（自動マッピング、または `[MapProperty]`。ドット付きパスも可）です。プロパティマッピングのないターゲット（何もマッピングしないもの、`[MapIgnore]` で除外したもの、`[MapConstant]`・`[MapExpression]`・`[MapUsing]`・`[MapFrom]`・`[MapNested]`・`[MapCollection]` が代入するもの）では何もしないため、診断されます（SMP0109）。見つからないターゲットも診断されます（SMP0102）。ターゲットは destination のプロパティやフィールド、そのドット付きパス、または戻り値のあるマッパーが呼ぶコンストラクタの引数です。
- メソッドは[属性が指すメソッド](#属性が指すメソッド)と同じように探して照合します。インスタンスのマッパーではインスタンスメソッドでもよく、ソースの値は[値の受け取り方](#値の受け取り方)のとおりに受け取り、`bool` を返します。オーバーロードは呼び出しが結び付くものを使い、あいまいな呼び出しや一致しないものは診断されます（SMP0112）。
- 引数が null を受け付けない条件メソッド（null 許容でない注釈の参照型、または `[DisallowNull]`）には null の source を渡さず、条件を満たさないものとして扱います（`if (source.Name is not null && IsShort(source.Name))`）。null 許容の構造体の source を、中の構造体や、中の値から暗黙に変換できる型で受け取る条件メソッドには `Value` を渡し、source が null なら条件を満たさないものとして扱います（`if (source.Count is not null && IsPositive(source.Count.Value))`）。
- ドット付きのソースの途中のメンバーが null のときは、調べるソースの値がないため、ターゲットをそのまま残します。
- コンストラクタやオブジェクト初期化子で代入されるメンバーは未代入のまま残せないため、`[MapCondition]` は指定できません（SMP0306）。

---

## Strict モード

`[Mapper]` か[プロファイル](#プロファイル)の `Strict = true` で、マッピングについての警告を有効にします。

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

### マップされていないメンバー

どのマッピングも代入しない destination のプロパティを警告します（SMP0501）。対象は、setter で（戻り値のあるマッパーではオブジェクト初期化子で設定する `init` アクセサーでも）マッパーが代入できるプロパティのうち、自動マッピングでも属性でも写さず、`[MapIgnore]` で除外もしていないものです。`[MapProperty("Child.Value", ...)]` の `Child` のように、ドット付きのターゲットのパスが中へ写すメンバーは、そのパスで写したものとして扱います。

戻り値のあるマッパーでは、コンストラクタでしか設定できないプロパティ（get だけ、またはマッパーのクラスから setter を呼べないもの）も、マッパーが呼べるコンストラクタの引数が受け取るのに、選んだ構築がその引数に値を渡さないときは対象です（[コンストラクタの選び方](#コンストラクタの選び方)）。ソースにあるかどうかは問いません。どのコンストラクタも受け取らないプロパティ（計算で求めるものなど）は対象外で、構築しない void マッパーでは、コンストラクタでしか設定できないプロパティと `init` 専用のプロパティは対象外です。省いた省略可能な引数が設定するはずのプロパティも、コンストラクタが既定値を与えますが対象です。`[MapIgnore]` を付ければ、既定値に任せたまま警告を消せます。自動マッピングが写さない `[Obsolete]` のプロパティは対象外です（[廃止されたメンバー](#廃止されたメンバー)）。

### null になりうる値

null を受け付けないターゲットに null になりうる値を入れ、ターゲットがそれに `null` か `default` を受け取るマッピングも警告します（SMP0502）。`NullValue`・`NullBehavior.Skip`・`[MapCondition]` でターゲットが受け取るものを指定したマッピングは対象外です。

null になりうる値は、宣言からそうと分かるものです。

- null 許容の型のソースのメンバー、またはプロパティか getter の戻り値に `[MaybeNull]` が付いたもの
- 式として値を作るところ（コンストラクタの引数やオブジェクト初期化子の項目。文ならそのときターゲットをそのまま残します）で、null 許容の型のメンバーを通して読む値
- `[MapFrom]` や `[MapUsing]` のメソッド、変換器、`[MapNested]` / `[MapCollection]` のマッパーが返し、生成コードが `!` を付けて受け取る null 許容の参照や、`[return: MaybeNull]` で null になりうるとした参照（null でない値を渡し、最初の引数を指定した `[return: NotNullIfNotNull]` でその値には null でない値を返すと分かるメソッドは除きます。生成したマッパーはこれを宣言します）
- null を受け付けないマッパーを呼ばない、null 許容の型の `[MapNested]` / `[MapCollection]` のソースや要素

null を受け付けないターゲットは、構造体か、`[AllowNull]` なしで null 非許容と注釈した参照（または `[DisallowNull]` 付きのもの）です。null 許容の注釈なし（null 許容コンテキストが無効）で宣言した参照は、null の検査では null 許容として扱いますが（[null 許容の注釈なし](#null-許容の注釈なし)）、ここでは対象外です。null かどうかを何も言っていないだけで、注釈なしで書いたモデルではすべてのメンバーが警告になるためです。

`Dst Map(Src? source)` のように、null 許容で宣言した source を受け取り、null を受け付けない型を返すマッパーは、source が null のとき `default` を返すため、メソッドの位置でターゲット `(return)` として警告します。

### 対応するメンバーのない enum のメンバー

enum を別の enum へ写すマッピングは、メンバーを名前で対応させます。ターゲットの enum に同じ名前のメンバーがないメンバーがソースの enum にあると警告します（SMP0503。そのメンバーを並べます）。その値はターゲットで `default` になり、null 許容の enum では `null` になります。メンバーを組み合わせた `[Flags]` の enum の値は来るまで分からないため、メンバーだけを見ます。マッピングに変換器を指定したものは、変換器が変換を引き受けるため対象外です。

### 警告の抑制

SMP0501 は、destination の構築についてのエラーと同じくマッパーのメソッドの位置で、SMP0502 と SMP0503 はマッピングの属性の位置（自動マッピングならメソッドの位置）で報告します（[診断を報告する位置](#診断を報告する位置)）。メソッドの位置で報告する警告はメソッドの最初の属性の位置から始まるため、`#pragma warning disable` はメソッドの属性より前に置いたときだけ効き、属性の位置で報告する警告はその属性より前に置いたときだけ効きます。属性とメソッドの間に置いても効きません。メソッドに付けた `[SuppressMessage]` はどちらにも効きます：

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

エラーは抑制できません。`#pragma warning disable`、`<NoWarn>`、`.editorconfig` やルールセットの重大度の設定、`[SuppressMessage]` のどれでもエラーは残るため、ビルドはエラーで止まり、エラーを報告したマッパーの `NotImplementedException` を投げる実装が動くことはありません（[診断を報告する位置](#診断を報告する位置)）。警告は上のとおり抑制できます。

---

## NativeAOT / トリミング

Smart.Mapper は NativeAOT および IL トリミングに完全対応しています。

- `Smart.Mapper.csproj` に `<IsAotCompatible>true</IsAotCompatible>` を宣言済み
- すべての型変換はスペシャライズドメソッドで完結 - 実行時のジェネリックリフレクションフォールバックなし。汎用のフォールバックが必要な変換は診断される（SMP0402）
- 生成コードは `Activator.CreateInstance` を使用しない。オブジェクト生成はジェネレーターがインライン展開（要素を `new()` で生成する `DefaultCollectionConverter` の `Action` オーバーロードには `RequiresUnreferencedCode` と `RequiresDynamicCode` を付与）
- `ValueConverterAttribute.ConverterType` と `CollectionConverterAttribute.ConverterType` に `[DynamicallyAccessedMembers]` 注釈（public のメソッドと public の入れ子の型）を付与済み

> **`[MapExpression]` の注意** - 式の中にリフレクション API（`Activator`・`Type.GetType`・`Assembly.Load`・`MethodInfo`・`PropertyInfo`・`FieldInfo`・`RuntimeHelpers.GetUninitializedObject`・`MakeGenericType`・`MakeGenericMethod`）が含まれる場合、属性の位置で SMP0403 が発行されます。AOT 環境では `[MapFrom]` または `[MapUsing]` への置き換えを検討してください。

---

## 診断

ジェネレーターは、コンパイル時の診断を、処理のフェーズごとの帯の ID で報告します。SMP00xx はマッパーメソッド、SMP01xx はマッピング属性とそれが指すメソッド、SMP02xx はメンバーのマッピングの機能、SMP03xx は構築、SMP04xx は変換と AOT、SMP05xx は Strict モードです。帯の中は、ジェネレーターが調べる順（エラーの後に警告）に番号を振っています。それぞれの原因と対処は [Diagnostics.md](../Diagnostics.md)（英語）を参照してください。

### 診断を報告する位置

原因が属性の診断は、その属性の位置で報告します。互いに食い違う 2 つの属性（SMP0101）では 2 つ目の属性、`[MapperProfile]`（クラスかアセンブリのもの。アセンブリのプロファイルのカルチャ名でない `Culture` はすべてのマッパーについて 1 回）やクラスの `[ValueConverter]` が与えた値が原因なら、その属性の位置です。static のマッパーが呼べないインスタンスメソッドを指す属性（SMP0105）、マッパーの引数が隠すメソッドを指す最初の属性（SMP0104）、`CultureInfo` の引数に代わられる `Culture` を指定した `[Mapper]`（SMP0404）も、その属性の位置で報告します。メソッドそのもの、自動マッピング、destination の構築（SMP0303・SMP0305・SMP0307・SMP0308）、Strict モードの未マップのプロパティ（SMP0501）についての診断はマッパーのメソッドの位置で、Strict モードのほかの警告（SMP0502・SMP0503）はマッピングの属性の位置（自動マッピングならメソッドの位置）で報告します。メソッドの位置で報告する警告はメソッドの最初の属性の位置から始まるため、`#pragma warning disable` は属性より前に置いたときだけ効きます（[警告の抑制](#警告の抑制)）。

エラーを報告したマッパーには、実装がない代わりに `NotImplementedException` を投げる実装を生成し、エラーに実装がないこと（CS8795）が並ばないようにします。エラーは抑制できず（[警告の抑制](#警告の抑制)）、ビルドはエラーで止まるため、この実装が動くことはありません。`partial` でないメソッドや、`partial` でない型・`file` の型の中のメソッド（SMP0001）は、生成コードが実装できないため生成しません。アクセシビリティ修飾子なしで宣言したメソッドには、宣言と同じく修飾子なしで生成します。

### 診断の一覧

| コード | 説明 | 重大度 |
|--------|------|--------|
| SMP0001 | マッパーメソッドは `partial` で、含む型もすべて `partial` で `file` の型でない必要がある | エラー |
| SMP0002 | マッパーメソッドが参照（`ref` / `ref readonly`）で返し、作った destination を返せない | エラー |
| SMP0003 | マッパーメソッドに引数がない、または `void` なのに source の後に宛先の引数がない | エラー |
| SMP0004 | マッパーメソッドのパラメーター名が `__` で始まっている（生成コードの予約名） | エラー |
| SMP0005 | 生成コードが扱えない修飾子がパラメーターに付いている（`out`、void マッパーの struct の宛先の修飾子なし・`in`・`ref readonly`。struct の宛先は `ref` で受け取る） | エラー |
| SMP0006 | 元の引数が null 許容の値型で、中の構造体のメンバーを持たない | エラー |
| SMP0007 | source や destination がコレクション・配列・配列やメモリーの要素のビュー（`Span<T>`・`Memory<T>`・`ArraySegment<T>` など）・タプル（またはそれに制約された型引数）で、マッパーは丸ごとは写さない（要素の型のマッパーで要素を写すか、コレクションを持つ型を `[MapCollection]` で写す） | エラー |
| SMP0008 | `CultureInfo` の引数が複数あり、変換のカルチャになる `culture` という名前のものがない | エラー |
| SMP0101 | 同一目的プロパティへのマッピングが重複している、メンバーとその中（`Child` と `Child.Value`）を両方マッピングしている、または同じターゲットに `[MapIgnore]` とマッピング属性を指定している | エラー |
| SMP0102 | マッピングのターゲットが見つからない、または代入できない（マッパーから呼べるセッターがない、`readonly` フィールド、null 許容の構造体を通るものなど、生成コードが通れないドット付きパス）。`[MapIgnore]` / `[MapCondition]` のターゲットも含む | エラー |
| SMP0103 | `[MapIgnore]` のターゲットがドット付きパス（メンバーの中のメンバー）で、自動マッピングが単独では代入しない | エラー |
| SMP0104 | マッパーの引数が、属性が指すメソッドと同じ名前で、生成コードがその名前で呼ぶメソッドを隠している（そのような最初の属性の位置で報告） | エラー |
| SMP0105 | static のマッパーが、マッパーのクラスにインスタンスメソッドしかない名前を指していて、呼べない | エラー |
| SMP0106 | `BeforeMap` メソッドのシグネチャが一致しない | エラー |
| SMP0107 | `AfterMap` メソッドのシグネチャが一致しない | エラー |
| SMP0108 | `[MapProperty]` のソースプロパティが見つからない | エラー |
| SMP0109 | `[MapCondition]` のターゲットに、条件が守るプロパティマッピングがない | エラー |
| SMP0110 | コンバーターメソッドが見つからない、またはシグネチャが一致しない（`[ValueConverter]` のクラスでは、カルチャか書式が当てはまるときのカルチャと書式を受け取るオーバーロードも） | エラー |
| SMP0111 | コンバーターの戻り値型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0112 | プロパティ条件メソッドのシグネチャが一致しない | エラー |
| SMP0201 | `MapUsing` メソッドのシグネチャが一致しない | エラー |
| SMP0202 | `MapUsing` メソッドの戻り値型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0203 | `[MapFrom]` ターゲットプロパティが目的型に存在しない | エラー |
| SMP0204 | `MapFrom` メンバーは、マッパーから呼べるソース型の引数なしメソッドまたはプロパティパス（継承したものを含む）である必要がある | エラー |
| SMP0205 | `MapFrom` メンバーの型が目的プロパティ型に暗黙に変換できない | エラー |
| SMP0206 | `[MapCollection]` / `[MapNested]` のソースプロパティが見つからない（ソースはソースの型のプロパティで、ドット付きのパスにはできない） | エラー |
| SMP0207 | `[MapCollection]` / `[MapNested]` のターゲットプロパティが見つからない | エラー |
| SMP0208 | `InPlace` の対象を空にして詰め直せない（`ICollection<T>` を実装しない、設計上読み取り専用、または構築前にインスタンスがない、コンストラクタが受け取るメンバーや `required` のメンバー） | エラー |
| SMP0209 | `[MapCollection]` / `[MapNested]` の対象に代入できない（マッパーから呼べるセッターも `init` アクセサーもない、または void マッパーで init 専用。`InPlace` は持っているインスタンスを詰め直す） | エラー |
| SMP0210 | `[MapCollection]` のソースプロパティがコレクション型ではない | エラー |
| SMP0211 | `[MapCollection]` のターゲットプロパティがコレクション型ではない | エラー |
| SMP0212 | `[MapCollection]` の対象が、生成コードの作るコレクションを受け取れない | エラー |
| SMP0213 | `MapCollection` 要素マッパーメソッドが見つからないまたはシグネチャが一致しない、または `Mapper` を指定していない | エラー |
| SMP0214 | `MapNested` マッパーメソッドが見つからないまたはシグネチャが一致しない、または `Mapper` を指定していない | エラー |
| SMP0215 | `[MapConstant]` の値や `NullValue` を生成コードに書けない（ファイルローカル型など） | エラー |
| SMP0216 | `[MapConstant]` の値や `NullValue` をターゲットの型に代入できない、または null を受け取らないターゲットに null を入れる | エラー |
| SMP0301 | 戻り値のあるマッパーが呼ぶコンストラクタが引数から代入するメンバーの中へ、ドット付きのターゲットを書いている | エラー |
| SMP0302 | `void` マッパーは `init` 専用メンバー（位置指定 `record` のプロパティ、ドット付きパスの末尾のものなど）やコンストラクタでしか代入されないメンバーに代入できない | エラー |
| SMP0303 | 戻り値のあるマッパーが destination を作れない（abstract、インターフェイス、マッパーから値を渡して呼べるコンストラクタがない、`new()` / `struct` の制約がない型パラメーター） | エラー |
| SMP0304 | destination をほかに作る方法がないコンストラクタの引数が代入するメンバーや、戻り値のあるマッパーが作る destination の `required` メンバーに `[MapIgnore]` を指定 | エラー |
| SMP0305 | destination をほかに作る方法がないコンストラクタの引数に値がない（一致するソースプロパティも属性もなく、省略可能でもない） | エラー |
| SMP0306 | コンストラクタ / 初期化子経由で代入されるターゲットに `[MapCondition]` / `NullBehavior.Skip` を指定 | エラー |
| SMP0307 | コンストラクタの引数が `required` メンバーを代入していて、オブジェクト初期化子で設定し直すことになる（コンストラクタに `[SetsRequiredMembers]` があれば対象外） | エラー |
| SMP0308 | 戻り値のあるマッパーが作る destination の `required` メンバー（プロパティまたはフィールド。アクセシビリティを問わず、継承したものを含む）がマップされていない（コンストラクタに `[SetsRequiredMembers]` があれば対象外） | エラー |
| SMP0401 | `Culture` がカルチャ名ではない（`""` はインバリアントカルチャ） | エラー |
| SMP0402 | AOT 非対応: 汎用 `Convert<TSource, TDestination>` フォールバックに到達する可能性がある（クラス・構造体・コレクションでは、`[MapNested]` / `[MapCollection]` を使うようメッセージで案内する） | エラー |
| SMP0403 | AOT 警告: `MapExpression` にリフレクションパターンが含まれる可能性がある | 警告 |
| SMP0404 | メソッドの null を受け付けない `CultureInfo` の引数がカルチャを決めるため、`[Mapper]` の `Culture` が使われない | 警告 |
| SMP0501 | Strict モード: マッパーから代入できる目的プロパティや、マッパーが呼べるコンストラクタでしか設定できない目的プロパティがマップされていない（マッパーのメソッドの位置で報告） | 警告 |
| SMP0502 | Strict モード: `NullValue`・`NullBehavior.Skip`・`[MapCondition]` なしで、null になりうる値を null を受け付けないターゲットに入れ、ターゲットが `null` か `default` を受け取る（属性の位置、自動マッピングならマッパーのメソッドの位置で報告）。null 許容で宣言した source から null を受け付けない型を返すマッパーも警告する（マッパーのメソッドの位置で、ターゲット `(return)` として報告） | 警告 |
| SMP0503 | Strict モード: 名前で対応させて写すターゲットの enum に、ソースの enum のメンバーと同じ名前のメンバーがない（属性の位置、自動マッピングならマッパーのメソッドの位置で報告） | 警告 |
