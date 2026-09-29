# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** は Roslyn Incremental Source Generator ベースの高性能オブジェクトマッパーライブラリです。
`[Mapper]` 属性を付与した `partial` メソッド（static でもインスタンスでも）に対して、プロパティコピーコードをコンパイル時に自動生成します。

## 特徴

- **ゼロオーバーヘッド** - リフレクションを一切使用しない静的コード生成
- **スペシャライズドメソッド方式** - `ConvertTo{TargetType}` 命名規則による直接呼び出し生成（JIT インライン展開と相性良好）
- **メソッド単位の宣言** - `[Mapper]` を個別メソッド（static でもインスタンスでも）に付与するため、通常のヘルパー関数と同じ感覚で扱え、インスタンスのマッパーはクラスに注入したサービスを使える
- **カスタムパラメーター透過** - `Map(Src, Dst, TContext ctx)` のような追加引数を、それを宣言した属性のメソッドや子・要素のマッパーに、順番を問わず型で（同じ型が複数なら名前で）渡し、`[MapExpression]` の式からも参照できる
- **カルチャに対応した変換** - 既定はインバリアントカルチャで、現在のカルチャ、カルチャ名、`CultureInfo` の引数も使え、日付と数値の書式も指定できる
- **NativeAOT / トリミング完全対応** - `<IsAotCompatible>true</IsAotCompatible>` 宣言済み・NativeAOT smoke test 通過済み
- **充実した診断** - フェーズ別採番（SMP0001〜SMP0503）の診断をコンパイル時に発行。それぞれの原因と対処は [Diagnostics.md](Diagnostics.md) に記載

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

## ドキュメント

- [API リファレンス](docs/API.ja.md) - すべての属性と規則の詳細。自動マッピング、プロパティパス、コレクション、コンストラクタ、null の扱い、型変換、カルチャ、Strict モード、細かな場合まで
- [診断](Diagnostics.md)（英語） - コンパイル時の診断それぞれの原因と対処
- [English](README.md)

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

生成コード：

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
}

public static partial Destination Map(Source source)
{
    var __d = new Destination();
    __d.Id          = source.Id;
    __d.Name        = source.Name;
    __d.Description = source.Description;
    return __d;
}
```

同名・互換型のプロパティは自動的にマッピングされ、下の属性でマッピングを変えられます。マッパーは拡張メソッド（`ToDestination(this Source source)`）にも、入れ子の型の中にも、ジェネリックにもできます。[マッパーメソッド](docs/API.ja.md#マッパーメソッド)を参照してください。

## 属性

| 属性 | 説明 |
|------|------|
| `[Mapper]` | マッパーメソッドの指定。`AutoMap`・`Strict`・`NameComparison`・`Culture`・`DateTimeFormat`・`NumberFormat` |
| `[MapperProfile]` | クラス、またはアセンブリのマッパーメソッドの既定値 |
| `[MapProperty]` / `[MapProperty<T>]` | ソースのメンバーやドット付きパスからターゲットへのマッピング。`Converter`・`NullValue`・`NullBehavior`・`Culture`・`DateTimeFormat`・`NumberFormat` |
| `[MapIgnore]` | destination のメンバーを自動マッピングから除外 |
| `[MapUsing]` | メソッドがソースから計算した値を代入 |
| `[MapFrom]` | ソースの引数なしメソッドの結果、またはプロパティパスを代入 |
| `[MapConstant]` / `[MapConstant<T>]` | 固定値を代入 |
| `[MapExpression]` | C# の式の値を代入（例: `"System.DateTime.Now"`） |
| `[MapCondition]` | 条件メソッドが `true` を返したときだけターゲットをマッピング |
| `[BeforeMap]` / `[AfterMap]` | マッピングの前 / 後にメソッドを呼ぶ |
| `[MapNested]` | マッパーメソッドでメンバーをマッピング |
| `[MapCollection]` | マッパーメソッドでコレクションを要素ごとにマッピング。`Strategy`・`Converter` |
| `[ValueConverter]` | カスタム型変換器のクラス（メソッド / クラスレベル） |
| `[CollectionConverter]` | カスタムコレクション変換器のクラス（メソッド / クラスレベル） |

destination のメンバーをマッピングする属性では、第1引数は **destination**（ターゲット）名です。すべてのプロパティは[属性リファレンス](docs/API.ja.md#属性リファレンス)を参照してください。

## 基本的な使い方

### インスタンスのマッパー

マッパーはインスタンスメソッドにもでき、その属性はクラスのフィールドを使うインスタンスメソッドを指せます。

```csharp
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

static のマッパーがインスタンスメソッドを指すと診断されます（SMP0105）。[static とインスタンスのマッパー](docs/API.ja.md#static-とインスタンスのマッパー)を参照してください。

### 名前の変更とネストしたプロパティ

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]   // 名前の変更
[MapProperty(nameof(Destination.City), "Address.City")]           // ネストしたソースのメンバーを展開
[MapProperty("Customer.Id", nameof(Source.CustomerId))]            // ネストしたターゲットのメンバーへ集約
public static partial void Map(Source source, Destination destination);
```

null 許容の途中のソースのメンバーは null を調べ、途中のターゲットのメンバーは null なら作ります。名前は `NameComparison`（既定は `Ordinal`）で比較します。[プロパティパス](docs/API.ja.md#プロパティパス)と[名前の比較方式](docs/API.ja.md#名前の比較方式)を参照してください。

### プロパティの除外

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
public static partial void Map(Source source, Destination destination);

[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]   // Id のみマッピング
public static partial void MapId(Source source, Destination destination);
```

[`[MapIgnore]`](docs/API.ja.md#mapignore) を参照してください。

### 計算した値と固定値

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]      // 値を計算するメソッド
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // ソースの引数なしメソッド
[MapConstant(nameof(Destination.Status), "Active")]                    // 固定値
[MapExpression(nameof(Destination.CreatedAt), "System.DateTime.Now")]  // C# の式
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

[`[MapUsing]`](docs/API.ja.md#mapusing)・[`[MapFrom]`](docs/API.ja.md#mapfrom)・[`[MapConstant]`](docs/API.ja.md#mapconstant--mapconstantt)・[`[MapExpression]`](docs/API.ja.md#mapexpression)・[代入の順序](docs/API.ja.md#代入の順序)を参照してください。

### null の扱い

```csharp
// Source: string? Name, string? Note / Destination: string Name, string Note
[Mapper]
[MapProperty(nameof(Destination.Name), NullValue = "Unknown")]              // source が null なら "Unknown"
[MapProperty(nameof(Destination.Note), NullBehavior = NullBehavior.Skip)]  // source が null なら値を残す
public static partial void Map(Source source, Destination destination);
```

どちらもなければ、source が null のときターゲットは `default` になります。[Null 処理](docs/API.ja.md#null-処理)を参照してください。

### 条件とコールバック

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(HasText))]
[BeforeMap(nameof(Prepare))]
[AfterMap(nameof(Complete))]
public static partial void Map(Source source, Destination destination);

private static bool HasText(string? value) => !string.IsNullOrEmpty(value);
private static void Prepare(Source source, Destination destination) { /* ... */ }
private static void Complete(Source source, Destination destination) { /* ... */ }
```

[コールバックと条件](docs/API.ja.md#コールバックと条件)を参照してください。

### 子オブジェクトとコレクション

```csharp
[Mapper]
public static partial ChildDto MapChild(Child source);

[Mapper]
[MapNested(nameof(ParentDto.Child), Mapper = nameof(MapChild))]
[MapCollection(nameof(ParentDto.Children), Mapper = nameof(MapChild))]
public static partial ParentDto Map(Parent source);
```

コレクションのループは、リスト・配列・集合・辞書・イミュータブルなコレクションに合わせてインラインで生成され、`Strategy = CollectionStrategy.InPlace` では新しいコレクションを代入する代わりに既存のコレクションを詰め直します。`List<ItemDto> Map(List<Item> source)` のようにコレクションを丸ごと写すマッパーは診断されます（SMP0007）。要素は要素の型のマッパーで写してください。[子オブジェクト](docs/API.ja.md#子オブジェクト)と[コレクション](docs/API.ja.md#コレクション)を参照してください。

### record とコンストラクタ

```csharp
public record OrderDto(int Id, string Name);

[Mapper]
public static partial OrderDto Map(Order source);   // new OrderDto(source.Id, source.Name)
```

戻り値のあるマッパーは、値のそろうコンストラクタを選んで呼び、`init` 専用と `required` のメンバーはオブジェクト初期化子で設定します。[コンストラクタと record](docs/API.ja.md#コンストラクタと-record) を参照してください。

### 型変換

組み込みの型と `string` の間、数値の間、enum の間（メンバー名で対応）、変換演算子や `Parse` による変換を生成します。

```csharp
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);   // string -> int
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);  // int -> string
```

`[ValueConverter]` のクラスはメソッドやクラスの変換器を置き換え、`[MapProperty]` の `Converter` は 1 つのプロパティを変換します。

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

[型変換](docs/API.ja.md#型変換)を参照してください。

### カルチャと書式

文字列との間の変換は、既定ではインバリアントカルチャを使います（数値は `InvariantCulture`、日付と時刻はラウンドトリップ書式 `O`）。カルチャ名、現在のカルチャ、`CultureInfo` の引数でこれを変えられ、`DateTimeFormat` / `NumberFormat` で書式を指定できます。

```csharp
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]   // カルチャ名がないときは現在のカルチャ

[MapperProfile(Culture = "ja-JP")]
internal static partial class AppMappers
{
    [Mapper(NumberFormat = "N2")]
    public static partial Dest Map(Src src);

    [Mapper]
    [MapProperty(nameof(Dest2.Amount), nameof(Src2.Price), Culture = "en-US", NumberFormat = "C")]
    public static partial Dest2 Map(Src2 src);

    [Mapper]
    public static partial Dest3 Map(Src3 src, CultureInfo culture);   // 呼び出し側がカルチャを渡す
}
```

カルチャは `[MapProperty]`、`CultureInfo` の引数、`[Mapper]`、クラスのプロファイル、アセンブリのプロファイル、最後に `DefaultCulture` の順に決まります。[カルチャと書式](docs/API.ja.md#カルチャと書式)を参照してください。

### プロファイル

クラスかアセンブリに付けた `[MapperProfile]` は、マッパーメソッドの既定値を与えます。`[Mapper]` がクラスのプロファイルより、クラスのプロファイルがアセンブリのプロファイルより優先され、設定ごとに独立に決まります。

```csharp
[assembly: MapperProfile(NameComparison = StringComparison.OrdinalIgnoreCase)]

[MapperProfile(Strict = true)]
internal static partial class OrderMappers
{
    [Mapper]
    public static partial OrderDto Map(Order source);
}
```

[プロファイル](docs/API.ja.md#プロファイル)を参照してください。

### Strict モード

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

Strict モードは、どのマッピングも代入しない destination のメンバー（SMP0501）、null を受け付けないターゲットへの null になりうる値（SMP0502）、ターゲットの enum に同じ名前のメンバーがない enum のメンバー（SMP0503）を警告します。[Strict モード](docs/API.ja.md#strict-モード)を参照してください。

### カスタムパラメーター

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

メソッドは宣言したカスタムパラメーターを、順番を問わず型で、同じ型が複数あるときは名前で受け取ります。`[MapNested]` / `[MapCollection]` のマッパーも受け取るため、`CultureInfo` の引数のカルチャが子オブジェクトにも渡ります。[カスタムパラメーター](docs/API.ja.md#カスタムパラメーター)を参照してください。

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

```powershell
# 単体テスト（Smart.Mapper.Tests。xUnit v3 と Microsoft Testing Platform）
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj

# コードカバレッジ付きで実行（出力フォルダーの TestResults に Cobertura XML を出力）
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj -- --coverage --coverage-settings CodeCoverage.runsettings

# ソースジェネレーターテスト（Smart.Mapper.Generator.Tests）: 生成コードと診断
dotnet run --project Smart.Mapper.Generator.Tests/Smart.Mapper.Generator.Tests.csproj

# NativeAOT スモークテスト（Smart.Mapper.AotTests）: 実行環境の RID（win-x64・linux-x64 など）で発行して実行
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64
.\Smart.Mapper.AotTests\bin\Release\net10.0\win-x64\publish\Smart.Mapper.AotTests.exe
```

.NET 10 SDK では Microsoft Testing Platform と VSTest の非互換により `dotnet test` を使えません。`dotnet run --project` か、Visual Studio のテストエクスプローラーを使ってください。AOT スモークテストは 8 つのシナリオ（基本の void マッピング、基本の戻り値マッピング、型変換、enum のマッピング、null の扱い、ネストプロパティマッピング、コレクションマッピング、カスタム型変換器）ごとに `[OK]` を出力し、最後に `All AOT smoke tests passed.` を出力します。失敗した場合は標準エラーに `FAIL: <message>` を出力し、非ゼロの終了コードで終了します。発行の出力に `IL2xxx` / `IL3xxx` の警告が出ないことも確認します（`dotnet publish ... 2>&1 | Select-String "IL2|IL3"`）。

---

## TODO

- **`FrozenSet` の直接構築** — 生成コードは `HashSet<T>` を構築してから `ToFrozenSet` を呼ぶ（BCL の設計上の二段構築）。BCL に frozen コレクションのビルダー API が追加されれば、中間セットを排除できる。
- **ジェネリックフォールバック `Convert<TSource, TDestination>` の `Half` / `Int128` / `UInt128` / `BigInteger` ソース対応** — ジェネリックコンバーターへのオプトイン経由では boxing フォールバックに到達する。既定の specialized メソッド経路はカバー済みのため、需要が生じた場合に分岐を追加する。
- **ジェネレーターのインクリメンタリティ** — モデルはマッパーメソッドごとに、ソースはクラスごとにキャッシュし、編集はマッパーが変わったクラスだけを生成し直す（プロパティの一覧、型の検索、変換器の検索はコンパイルの中で共有する）。モデルのクラスごとへの振り分けは編集のたびにすべてのモデルを通る（`Collect()`）が、現状のコストは小さく、非常に大きなプロジェクトで必要になれば、さらに細かく分割する。

---

## ライセンス

MIT
