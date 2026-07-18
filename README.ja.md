# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** は Roslyn Incremental Source Generator ベースの高性能オブジェクトマッパーライブラリです。
`[Mapper]` 属性を付与した `static partial` メソッドに対して、プロパティコピーコードをコンパイル時に自動生成します。

## 特徴

- **ゼロオーバーヘッド** - リフレクションを一切使用しない静的コード生成
- **スペシャライズドメソッド方式** - `ConvertTo{TargetType}` 命名規則による直接呼び出し生成（JIT インライン展開と相性良好）
- **メソッド単位の宣言** - `[Mapper]` を個別メソッドに付与するため、通常のヘルパー関数と同じ感覚で扱える
- **カスタムパラメーター透過** - `Map(Src, Dst, TContext ctx)` のように追加引数をすべてのフックに透過的に伝播
- **NativeAOT / トリミング完全対応** - `<IsAotCompatible>true</IsAotCompatible>` 宣言済み・NativeAOT smoke test 通過済み
- **充実した診断** - フェーズ別採番の 28 種（SMP0001〜SMP0501）をコンパイル時に発行

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
    var destination = new Destination();
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
    return destination;
}
```

---

## 属性リファレンス

### メソッドレベル属性

| 属性 | 説明 |
|------|------|
| `[Mapper]` | マッピングメソッドの指定 |
| `[Mapper(AutoMap = false)]` | 自動マッピングの無効化 |
| `[Mapper(Strict = true)]` | 未マップ destination プロパティを警告（SMP0501） |
| `[Mapper(NameComparison = ...)]` | 自動マッピング時のプロパティ名比較方式（既定: `Ordinal`） |
| `[Mapper(Culture = "...")]` | 型変換時に使用するカルチャ（例: `"ja-JP"`） |
| `[Mapper(DateTimeFormat = "...")]` | `DateTime` <-> `string` 変換時のフォーマット（`Culture` と共に使用） |
| `[Mapper(NumberFormat = "...")]` | 数値型 <-> `string` 変換時のフォーマット（`Culture` と共に使用） |
| `[MapProperty]` | プロパティ間の明示的マッピング。`NullSubstitute`・`Culture`・`DateTimeFormat`・`NumberFormat`・`Converter` 対応 |
| `[MapProperty<T>]` | 型安全版 `[MapProperty]`（C# 11+） |
| `[MapUsing]` | 静的メソッドによる値の計算（カスタムパラメーター対応） |
| `[MapFrom]` | ソースオブジェクトのインスタンスメソッド呼び出し、またはドット記法プロパティパス |
| `[MapConstant]` | 固定値の設定 |
| `[MapConstant<T>]` | 型安全版 `[MapConstant]`（C# 11+） |
| `[MapExpression]` | 任意の C# 式を埋め込む（例: `"System.DateTime.Now"`） |
| `[MapIgnore]` | プロパティのマッピングを除外 |
| `[BeforeMap]` | マッピング前のコールバック |
| `[AfterMap]` | マッピング後のコールバック |
| `[MapCondition]` | 条件付きマッピング（グローバルまたはプロパティ単位） |
| `[MapCollection]` | 明示的マッパーメソッドを使ったコレクションマッピング |
| `[MapNested]` | 明示的マッパーメソッドを使ったネストオブジェクトマッピング |
| `[ValueConverter]` | カスタム型変換器（メソッド / クラスレベル） |
| `[CollectionConverter]` | カスタムコレクション変換器（メソッド / クラスレベル） |

> **第1引数の規則** - すべての属性で、第1引数は **destination**（ターゲット）プロパティ名、第2引数は source です。

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

### Null 代替値

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullSubstitute = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullSubstitute = 0)]
public static partial void Map(Source source, Destination destination);
```

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

グローバル条件:

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMap(Source source, Destination destination) => source.IsActive;
```

プロパティレベル条件:

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

### 自動マッピング無効化（`AutoMap = false`）

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Source.Id), nameof(Destination.Id))]
public static partial void Map(Source source, Destination destination);
// Id のみマッピング。他のプロパティは無視。
```

---

## ネストプロパティマッピング

`[MapProperty]` のドット記法でネストプロパティの展開・集約が可能です。

### Flatten（ネスト source → フラット destination）

```csharp
[Mapper]
[MapProperty("Child.Id",   "ChildId")]
[MapProperty("Child.Name", "ChildName")]
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
[MapProperty("Value1", "Child1.Value")]
[MapProperty("Value2", "Child2.Value")]
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
    [MapCollection(nameof(Destination.Children), nameof(Source.Children), MapperMethod = nameof(MapChild))]
    public static partial void Map(Source source, Destination destination);
}
```

生成コード：

```csharp
destination.Children = global::Smart.Mapper.DefaultCollectionConverter.ToList<SourceChild, DestinationChild>(
    source.Children, MapChild)!;
```

`DefaultCollectionConverter` は関数マッパー・アクションマッパーどちらにも対応した `ToArray` / `ToList` オーバーロードを提供します。ソースコレクションが null の場合は `default` を返します。

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

```csharp
public record DestModel(int Id, string Name);

[Mapper]
public static partial DestModel Map(SrcModel src);
```

生成コード：

```csharp
public static partial DestModel Map(SrcModel src)
{
    var destination = new DestModel(src.Id, src.Name);
    return destination;
}
```

> `init-only` / `record` destination に対して `void` マッパーは使用できません（SMP0302）。

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
    ? DefaultValueConverter.ConvertToString(source.NullableValue.Value)
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
[MapCollection(nameof(Source.Items), nameof(Destination.Items), MapperMethod = nameof(MapItem))]
public static partial void Map(Source source, Destination destination);
```

---

## Culture / Format

```csharp
[MapperProfile(Culture = "ja-JP")]
internal static partial class AppMappers
{
    [Mapper(Culture = "de-DE", NumberFormat = "N2")]
    public static partial Dest Map(Src src);

    [Mapper]
    [MapProperty(nameof(Dst.Amount), nameof(Src.Price), Culture = "en-US", NumberFormat = "C")]
    public static partial Dest2 Map(Src2 src);
}
```

優先順位: `[MapProperty]` > `[Mapper]` > `[MapperProfile]` > `CultureInfo.InvariantCulture`

解決された `CultureInfo` は生成クラス内で `static readonly` フィールドとしてキャッシュされ、変換ごとの `GetCultureInfo(...)` 呼び出しコストを排除します。

> `Culture` なしで `DateTimeFormat` / `NumberFormat` のみ指定するとコンパイルエラー（SMP0401）。

---

## NativeAOT / トリミング対応

Smart.Mapper は NativeAOT および IL トリミングに完全対応しています。

- `Smart.Mapper.csproj` に `<IsAotCompatible>true</IsAotCompatible>` を宣言済み
- すべての型変換はスペシャライズドメソッドで完結 - 実行時のジェネリックリフレクションフォールバックなし
- `Activator.CreateInstance` は使用しない。オブジェクト生成はジェネレーターがインライン展開
- `ValueConverterAttribute.ConverterType` と `CollectionConverterAttribute.ConverterType` に `[DynamicallyAccessedMembers]` 注釈付与済み

> **`[MapExpression]` の注意** - 式の中にリフレクション API（`Activator`・`Type.GetType`・`MethodInfo` など）が含まれる場合、SMP0403 が発行されます。AOT 環境では `[MapFrom]` または `[MapUsing]` への置き換えを検討してください。

---

## 診断メッセージ

| コード | 説明 | 重大度 |
|--------|------|--------|
| SMP0001 | マッパーメソッドは `static partial` である必要がある | エラー |
| SMP0002 | マッパーメソッドのパラメーター数が無効 | エラー |
| SMP0003 | カスタムパラメーターの型が重複している | エラー |
| SMP0101 | 同一目的プロパティへのマッピングが重複している | エラー |
| SMP0102 | `BeforeMap` メソッドのシグネチャが一致しない | エラー |
| SMP0103 | `AfterMap` メソッドのシグネチャが一致しない | エラー |
| SMP0104 | コンバーターメソッドのシグネチャが一致しない | エラー |
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
| SMP0212 | `[MapCollection]` / `[MapNested]` は init 専用 / required メンバーを対象にできない | エラー |
| SMP0301 | コンストラクターパラメーターに一致するソースプロパティがない | エラー |
| SMP0302 | `init` 専用 / `record` 型の目的に `void` マッパーは使用不可 | エラー |
| SMP0303 | `required` メンバーがマップされていない | エラー |
| SMP0401 | `Culture` なしで `DateTimeFormat` / `NumberFormat` を指定している | エラー |
| SMP0402 | AOT 非対応: 汎用 `Convert<TSource,TDest>` フォールバックに到達する可能性がある | エラー |
| SMP0403 | AOT 警告: `MapExpression` にリフレクションパターンが含まれる可能性がある | 警告 |
| SMP0501 | Strict モード: マップされていない目的プロパティがある | 警告 |

---

## ベンチマーク

[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) を使用して .NET 10 上で計測。

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.300  [Host / MediumRun] : .NET 10.0.8, X64 RyuJIT x86-64-v3
Job=MediumRun  IterationCount=15  LaunchCount=2  WarmupCount=10
```

### 単純マッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 7.760 ns | 0.238 ns | 0.356 ns | 1.00 | 64 B |
| SmartMapper | 8.736 ns | 0.334 ns | 0.499 ns | 1.13 | 64 B |

### 型変換マッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 77.36 ns | 1.149 ns | 1.684 ns | 1.00 | 128 B |
| SmartMapper | 77.96 ns | 1.183 ns | 1.771 ns | 1.01 | 128 B |

### ネストマッピング

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 8.993 ns | 0.254 ns | 0.380 ns | 1.00 | 72 B |
| SmartMapper | 8.887 ns | 0.263 ns | 0.385 ns | 0.99 | 72 B |

### コレクションマッピング

| Method | ItemCount | Mean | Ratio | Allocated |
|--------|----------:|-----:|------:|----------:|
| Direct | 10 | 77.90 ns | 1.00 | 456 B |
| SmartMapper | 10 | 70.76 ns | 0.91 | 512 B |
| Direct | 100 | 623.78 ns | 1.00 | 4056 B |
| SmartMapper | 100 | 561.97 ns | 0.90 | 4112 B |

> Smart.Mapper の生成コードは手書きコードとほぼ同等の命令列にコンパイルされます。単純マッピングでの微小なオーバーヘッドはメソッド呼び出し境界によるもので、JIT インライン展開により通常は解消されます。

---

## テストの実行

### 単体テスト（`Smart.Mapper.Tests`）

xUnit v3 と Microsoft Testing Platform を使用します。

```powershell
# 全テストを実行
dotnet test Smart.Mapper.Tests/Smart.Mapper.Tests.csproj

# コードカバレッジ付きで実行
dotnet test Smart.Mapper.Tests/Smart.Mapper.Tests.csproj --settings CodeCoverage.runsettings
```

Visual Studio のテストエクスプローラーからも実行できます。

### ソースジェネレーターテスト（`Smart.Mapper.Generator.Tests`）

Roslyn ソースジェネレーターが正しい出力と診断を生成することを検証します。

```powershell
dotnet test Smart.Mapper.Generator.Tests/Smart.Mapper.Generator.Tests.csproj
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
