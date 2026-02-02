# MapperLibrary 仕様設計書

## 1. 概要

MapperLibraryは、Source Generatorベースのオブジェクトマッパーライブラリです。
SourceオブジェクトのプロパティをDestinationオブジェクトのプロパティにコピーするコードを自動生成します。

## 2. 基本設計

### 2.1 基本的なマッピング定義

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);

[Mapper]
public static partial Destination Map(Source source);
```

- 同名・同型のプロパティは自動でマッピング
- `partial`メソッドに対してSource Generatorがコードを生成

**生成コード例（voidパターン）:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id = source.Id;
    destination.Name = source.Name;
    destination.Description = source.Description;
}
```

**生成コード例（戻り値パターン）:**

```csharp
public static partial Destination Map(Source source)
{
    var destination = new Destination();
    destination.Id = source.Id;
    destination.Name = source.Name;
    destination.Description = source.Description;
    return destination;
}
```

## 3. 属性一覧

### 3.1 メソッドレベル属性

| 属性 | 用途 | 適用対象 |
|------|------|----------|
| `[Mapper]` | マッピングメソッドの指定 | Method |
| `[Mapper(AutoMap = false)]` | 自動マッピング無効化 | Method |
| `[MapProperty]` | プロパティ間のマッピング指定 | Method |
| `[MapUsing]` | 静的メソッドによる値の計算（カスタムパラメーター対応） | Method |
| `[MapFrom]` | ソースオブジェクトのメソッド呼び出しまたはプロパティパス | Method |
| `[MapConstant]` | 固定値の設定 | Method |
| `[MapConstant<T>]` | 型指定の固定値設定（C# 11+） | Method |
| `[MapExpression]` | 動的式の設定（例: DateTime.Now） | Method |
| `[MapIgnore]` | マッピング除外 | Method |
| `[AfterMap]` | マッピング後の追加処理 | Method |
| `[BeforeMap]` | マッピング前の追加処理 | Method |
| `[MapCondition]` | マッピング条件（グローバルまたはプロパティ単位） | Method |
| `[MapCollection]` | コレクションマッピング | Method |
| `[MapNested]` | 子オブジェクトマッピング | Method |
| `[MapConverter]` | カスタム型変換器 | Method, Class |
| `[CollectionConverter]` | カスタムコレクション変換器 | Method, Class |

### 3.2 属性パラメーターの順序

すべての属性で、第1引数は**ターゲット**（destination）プロパティ名です：

```csharp
[MapProperty("TargetProperty", "SourceProperty")]     // Target first
[MapUsing("TargetProperty", "ComputeMethod")]         // Target first
[MapFrom("TargetProperty", "SourceMethodOrPath")]     // Target first
[MapCollection("TargetProperty", "SourceProperty")]   // Target first
[MapNested("TargetProperty", "SourceProperty")]       // Target first
```

## 4. 詳細仕様

### 4.1 異なるプロパティ名間のマッピング

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [MapProperty(nameof(Destination.DestinationName), nameof(Source.SourceName))]
    [MapProperty(nameof(Destination.Id), nameof(Source.Code))]  // 複数指定可能
    public static partial void Map(Source source, Destination destination);
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.DestinationName = source.SourceName;
    destination.Id = source.Code;
}
```

### 4.2 複数プロパティからの値合成（MapFrom）

`[MapFrom]` 属性を使用して、同じクラス内の静的メソッドを呼び出し、その結果をターゲットプロパティに設定します。

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [MapFrom("FullName", nameof(CombineFullName))]
    public static partial void Map(Source source, Destination destination);
    
    // 合成用メソッド（同じクラス内に定義）
    private static string CombineFullName(Source source) 
        => $"{source.FirstName} {source.LastName}";
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.FullName = CombineFullName(source);
    // 他のプロパティ...
}
```

#### カスタムパラメーター対応

マッパーメソッドにカスタムパラメーターがある場合、MapFromメソッドにもカスタムパラメーターを渡すことができます。

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [MapFrom("FullName", nameof(CombineFullName))]
    public static partial Destination Map(Source source, FormattingContext context);
    
    // カスタムパラメーターを受け取るメソッド
    private static string CombineFullName(Source source, FormattingContext context) 
        => $"{source.FirstName}{context.Separator}{source.LastName}";
}
```

**注意:**
- メソッドの戻り値の型は、ターゲットプロパティの型と一致する必要があります
- メソッドが見つからない場合、または型が一致しない場合はコンパイルエラーになります

### 4.2.1 ソースオブジェクトのメソッド呼び出しまたはプロパティパス（MapFrom）

`[MapFrom]` 属性を使用して、ソースオブジェクトのメソッド呼び出しまたはプロパティパスからターゲットプロパティに値を設定します。

```csharp
public class Source
{
    public int[] Items { get; set; } = [];
    public NestedObject Nested { get; set; } = new();
    
    // 引数なしのインスタンスメソッド
    public int GetItemCount() => Items.Length;
    public int GetItemSum() => Items.Sum();
}

public class NestedObject
{
    public int Value { get; set; }
}

public class Destination
{
    public int ItemCount { get; set; }
    public int ItemSum { get; set; }
    public int NestedValue { get; set; }
}

internal static partial class ObjectMapper
{
    [Mapper]
    [MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // メソッド呼び出し
    [MapFrom(nameof(Destination.ItemSum), nameof(Source.GetItemSum))]      // メソッド呼び出し
    [MapFrom(nameof(Destination.NestedValue), "Nested.Value")]             // プロパティパス
    public static partial void Map(Source source, Destination destination);
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.ItemCount = source.GetItemCount();
    destination.ItemSum = source.GetItemSum();
    destination.NestedValue = source.Nested.Value;
}
```

**注意:**
- メソッド呼び出しの場合、ソースメソッドは引数なしのインスタンスメソッドである必要があります
- プロパティパスの場合、ドット区切りでネストしたプロパティにアクセスできます
- 戻り値または最終プロパティの型は、ターゲットプロパティの型と一致する必要があります
- 引数が必要な計算の場合は `[MapUsing]` を使用してください

### 4.2.2 自動マッピングの無効化（AutoMap = false）

`[Mapper(AutoMap = false)]` を使用すると、同名プロパティの自動マッピングを無効化し、明示的に指定したプロパティのみをマッピングします。

```csharp
public class Source
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}

public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Value { get; set; }
}

internal static partial class ObjectMapper
{
    // AutoMap = false: 明示的に指定したプロパティのみマッピング
    [Mapper(AutoMap = false)]
    [MapProperty(nameof(Source.Id), nameof(Destination.Id))]
    public static partial void Map(Source source, Destination destination);
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    // Idのみマッピング（NameとValueは自動マッピングされない）
    destination.Id = source.Id;
}
```

**使用ケース:**
- AutoMapperの `.ForAllMembers(opt => opt.Ignore())` + 明示的マッピングと同等の動作
- AfterMapで手動設定のみ行い、自動マッピングを無効化したい場合

### 4.3 追加処理（Before/After Map）

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    [BeforeMap(nameof(BeforeMapping))]
    [AfterMap(nameof(AfterMapping))]
    public static partial void Map(Source source, Destination destination);
    
    private static void BeforeMapping(Source source, Destination destination)
    {
        // マッピング前の処理
    }
    
    private static void AfterMapping(Source source, Destination destination)
    {
        // マッピング後の処理
    }
}
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    BeforeMapping(source, destination);
    
    destination.Value1 = source.Value1;
    // ...
    
    AfterMapping(source, destination);
}
```

### 4.4 入れ子クラスのマッピング

#### パターンA: 自動検出（同じクラス内にマッパーが存在する場合）

```csharp
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);
    
    [Mapper]
    public static partial void Map(NestedSource source, NestedDestination destination);
}
```

生成時に、`NestedSource` → `NestedDestination`のマッパーが同一クラス内にあれば自動的に使用。

#### パターンB: 明示的指定

```csharp
[Mapper]
[MapProperty("Nested", "Nested", MapperType = typeof(NestedObjectMapper))]
public static partial void Map(Source source, Destination destination);
```

### 4.5 型変換（カスタムコンバーター）

#### 4.5.1 デフォルト動作

- 組み込み型間: `Convert.ToXxx()` または暗黙的変換を使用
- `int` → `string`: `ToString()` を使用
- `string` → `int`: `int.Parse()` または `Convert.ToInt32()` を使用

#### 4.5.2 カスタムコンバーター定義

```csharp
// コンバータークラス
public static class CustomConverters
{
    public static string IntToFormattedString(int value) 
        => value.ToString("N0");
    
    public static DateTime StringToDateTime(string value) 
        => DateTime.Parse(value);
}
```

#### 4.5.3 コンバーター指定方法

**メソッドレベル（特定のプロパティ）:**

```csharp
[Mapper]
[MapProperty("Value", "FormattedValue", Converter = nameof(CustomConverters.IntToFormattedString))]
public static partial void Map(Source source, Destination destination);
```

**クラスレベル（型ペアに対して）:**

```csharp
[MapperConverter(typeof(int), typeof(string), typeof(CustomConverters), nameof(CustomConverters.IntToFormattedString))]
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);
}
```

**アセンブリレベル（グローバル）:**

```csharp
[assembly: MapperConverter(typeof(int), typeof(string), typeof(CustomConverters), nameof(CustomConverters.IntToFormattedString))]
```

### 4.6 固定値の設定

```csharp
[Mapper]
[MapConstant("Status", "Active")]
[MapConstant("Version", 1)]
[MapConstant("CreatedAt", Expression = "DateTime.Now")]  // 式として評価
public static partial void Map(Source source, Destination destination);
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Status = "Active";
    destination.Version = 1;
    destination.CreatedAt = DateTime.Now;
    // ...
}
```

## 5. マッピング除外

```csharp
[Mapper]
[MapIgnore("InternalId")]
[MapIgnore("TempValue")]
public static partial void Map(Source source, Destination destination);
```

**生成コード例:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    // InternalId と TempValue はマッピングされない
    destination.Name = source.Name;
    // ...
}
```

## 6. ネストプロパティマッピング

`[MapProperty]`属性でドット記法を使用することで、ネストされたプロパティ間のマッピングが可能です。

### 6.1 フラットからネストへのマッピング（Unflatten）

```csharp
public class Source
{
    public int Value1 { get; set; }
    public int Value2 { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class Destination
{
    public DestinationChild? Child1 { get; set; }
    public DestinationChild? Child2 { get; set; }
}

[Mapper]
[MapProperty("Value1", "Child1.Value")]
[MapProperty("Value2", "Child2.Value")]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Child1 ??= new DestinationChild();
    destination.Child2 ??= new DestinationChild();
    destination.Child1.Value = source.Value1;
    destination.Child2.Value = source.Value2;
}
```

### 6.2 ネストからフラットへのマッピング（Flatten）

```csharp
public class SourceChild
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Source
{
    public SourceChild? Child { get; set; }
}


public class Destination
{
    public int ChildId { get; set; }
    public string ChildName { get; set; }
}

[Mapper]
[MapProperty("Child.Id", "ChildId")]
[MapProperty("Child.Name", "ChildName")]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    if (source.Child is not null)
    {
        destination.ChildId = source.Child.Id;
        destination.ChildName = source.Child.Name;
    }
}
```

### 6.3 深いネストのマッピング

```csharp
[Mapper]
[MapProperty("DeepValue", "Outer.Inner.Value")]
public static partial void Map(DeepSource source, DeepNestedDestination destination);
```

**生成コード:**

```csharp
public static partial void Map(DeepSource source, DeepNestedDestination destination)
{
    destination.Outer ??= new DeepNestedParent();
    destination.Outer.Inner ??= new DeepNestedChild();
    destination.Outer.Inner.Value = source.DeepValue;
}
```

### 6.4 注意事項

- **ターゲット側のネスト**: 中間オブジェクトが`null`の場合、自動的にインスタンスが生成されます（`??= new`）
- **ソース側のネスト**: 中間オブジェクトがnullableの場合、nullチェックが追加され、nullの場合はマッピングがスキップされます
- 既存のインスタンスは保持され、プロパティのみが更新されます

## 7. Null処理

### 7.1 Nullableプロパティの動作

| ソース型 | ターゲット型 | 動作 |
|----------|-------------|------|
| `T?` | `T?` | そのままコピー（nullも含む） |
| `T?` | `T` (末端) | `default!` を代入 |
| `T` | `T?` | そのままコピー |
| `T` | `T` | そのままコピー |

### 7.2 ネストプロパティのnull処理

ソース側のネストプロパティの**中間パス**がnullableの場合、処理をスキップします：

```csharp
// Source.Child? がnullableの場合
public static partial void Map(Source source, Destination destination)
{
    // 非ネストプロパティは常にコピー
    destination.DirectValue = source.DirectValue;
    
    // ネストプロパティは中間要素のnullチェック付き
    if (source.Child is not null)
    {
        destination.ChildId = source.Child.Id;
        destination.ChildName = source.Child.Name;
    }
}
```

### 7.3 末端要素の nullable → non-nullable マッピング

末端の要素がnullの場合は `default!` を代入します：

```csharp
public class Source
{
    public string? Name { get; set; }
    public int? IntValue { get; set; }
}

public class Destination
{
    public string Name { get; set; } = "default";
    public string IntValue { get; set; } = "default";
}
```


**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    // string? -> string: null-forgiving operator を使用
    destination.Name = source.Name!;
    
    // int? -> string: null-coalescing で default! を使用
    destination.IntValue = source.IntValue?.ToString() ?? default!;
}
```

## 8. 実装済み型変換一覧

### 8.1 文字列への変換

すべての型から `string` への変換は `ToString()` メソッドを使用します。
nullable型の場合は `?.ToString()` を使用し、ターゲットがnon-nullableなら `?? default!` を追加します。

### 8.2 文字列からの変換

| 変換先 | 変換方法 |
|--------|----------|
| `int` | `int.Parse()` |
| `long` | `long.Parse()` |
| `short` | `short.Parse()` |
| `byte` | `byte.Parse()` |
| `uint`, `ulong`, `ushort` | 対応する `Parse()` |
| `float` | `float.Parse()` |
| `double` | `double.Parse()` |
| `decimal` | `decimal.Parse()` |
| `bool` | `bool.Parse()` |
| `DateTime` | `DateTime.Parse()` |
| `DateTimeOffset` | `DateTimeOffset.Parse()` |
| `DateOnly` | `DateOnly.Parse()` |
| `TimeOnly` | `TimeOnly.Parse()` |
| `TimeSpan` | `TimeSpan.Parse()` |
| `Guid` | `Guid.Parse()` |

### 8.3 数値型間の変換

数値型（`int`, `long`, `short`, `byte`, `float`, `double`, `decimal` 等）間の変換は、明示的なキャストを使用します。

### 8.4 日時型の変換

| 変換元 | 変換先 | 変換方法 |
|--------|--------|----------|
| `DateTime` | `DateTimeOffset` | `new DateTimeOffset(value)` |
| `DateTimeOffset` | `DateTime` | `.DateTime` |
| `DateTime` | `DateOnly` | `DateOnly.FromDateTime()` |
| `DateTime` | `TimeOnly` | `TimeOnly.FromDateTime()` |

## 9. カスタムパラメータ

マッパーメソッドに追加のパラメータを指定し、`BeforeMap`/`AfterMap`などのカスタムメソッドで使用できます。

### 9.1 パラメータの識別ルール

| パターン | Source | Destination | カスタムパラメータ |
|---------|--------|-------------|-------------------|
| `void Map(A, B, C, D)` | 第1引数 | 第2引数 | 第3引数以降 |
| `B Map(A, C, D)` | 第1引数 | 戻り値 | 第2引数以降 |

### 9.2 使用例

```csharp
// カスタムコンテキストを渡すパターン
[Mapper]
[BeforeMap(nameof(OnBeforeMap))]
[AfterMap(nameof(OnAfterMap))]
public static partial void Map(Source source, Destination destination, IServiceProvider services);

// カスタムパラメータを受け取るコールバック
private static void OnBeforeMap(Source source, Destination destination, IServiceProvider services)
{
    // servicesを使った処理
}

// カスタムパラメータなしのコールバック（後方互換）
private static void OnAfterMap(Source source, Destination destination)
{
    // 基本的な処理
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination, IServiceProvider services)
{
    OnBeforeMap(source, destination, services);  // カスタムパラメータを渡す
    destination.Name = source.Name;
    // ...
    OnAfterMap(source, destination);  // カスタムパラメータなしで呼び出し
}
```

### 9.3 戻り値パターン

```csharp
[Mapper]
[AfterMap(nameof(OnAfterMap))]
public static partial Destination Map(Source source, CustomContext context);

private static void OnAfterMap(Source source, Destination destination, CustomContext context)
{
    context.MappingComplete = true;
}
```

### 9.4 制約事項

- **同じ型の複数パラメータは禁止**: カスタムパラメータに同じ型を複数指定するとコンパイルエラー（ML0003）

```csharp
// NG: 同じ型 (string) が複数ある
[Mapper]
public static partial void Map(Source source, Destination destination, string param1, string param2);
// → ML0003: Custom parameters must have unique types
```

### 9.5 BeforeMap/AfterMap のシグネチャ

| BeforeMap/AfterMapのシグネチャ | 動作 |
|------------------------------|------|
| `(Source, Destination)` | カスタムパラメータなしで呼び出し |
| `(Source, Destination, ...customParams)` | カスタムパラメータを渡して呼び出し |
| シグネチャ不一致 | コンパイルエラー（ML0004/ML0005） |

カスタムパラメータを持つバージョンが優先されます。

### 9.6 Converter でのカスタムパラメータ

`MapProperty`の`Converter`プロパティで指定したカスタムコンバーターでもカスタムパラメータを使用できます。

```csharp
[Mapper]
[MapProperty(nameof(Source.Value), nameof(Destination.ConvertedValue), Converter = nameof(ConvertWithContext))]
public static partial void Map(Source source, Destination destination, CustomContext context);

// カスタムパラメータなしのコンバーター
private static string ConvertIntToString(int value)
{
    return $"Value: {value}";
}

// カスタムパラメータありのコンバーター（優先される）
private static string ConvertWithContext(int value, CustomContext context)
{
    return $"Value: {value}, Context: {context.Value}";
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination, CustomContext context)
{
    destination.ConvertedValue = ConvertWithContext(source.Value, context);
}
```

| Converterのシグネチャ | 動作 |
|---------------------|------|
| `(SourceType)` | カスタムパラメータなしで呼び出し |
| `(SourceType, ...customParams)` | カスタムパラメータを渡して呼び出し |
| シグネチャ不一致 | コンパイルエラー（ML0006） |

## 10. 条件付きマッピング

### 10.1 グローバル条件

マッピング全体に条件を適用します。

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMap(Source source, Destination destination)
{
    return source.IsActive;
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    if (ShouldMap(source, destination))
    {
        destination.Value = source.Value;
        destination.Name = source.Name;
    }
}
```

### 10.2 プロパティレベル条件

特定のプロパティのマッピングに条件を適用します。

```csharp
[Mapper]
[MapPropertyCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name)
{
    return !string.IsNullOrEmpty(name);
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Value = source.Value;
    if (ShouldMapName(source.Name))
    {
        destination.Name = source.Name;
    }
}
```

### 10.3 カスタムパラメータ付き条件

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination, MappingContext context);

private static bool ShouldMap(Source source, Destination destination, MappingContext context)
{
    return context.ShouldMap && source.IsActive;
}
```

## 11. Generic MapConstant

C# 11以降のGeneric Attribute機能を使用して、型安全な定数マッピングができます。

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Name = source.Name;
    destination.Version = 1;
    destination.Status = "Active";
    destination.IsEnabled = true;
}
```

従来の非Generic版も引き続き使用できます：

```csharp
[MapConstant("Status", "Active")]
[MapConstant("Version", 1)]
[MapConstant("CreatedAt", null, Expression = "System.DateTime.Now")]
```

## 12. 診断メッセージ

| コード | 説明 |
|--------|------|
| ML0001 | Mapperメソッドは static partial である必要があります |
| ML0002 | Mapperメソッドのパラメータ数が不正です |
| ML0003 | カスタムパラメータに同じ型が複数指定されています |
| ML0004 | BeforeMapメソッドのシグネチャが一致しません |
| ML0005 | AfterMapメソッドのシグネチャが一致しません |
| ML0006 | Converterメソッドのシグネチャが一致しません |
| ML0007 | 条件メソッドのシグネチャが一致しません |
| ML0008 | プロパティ条件メソッドのシグネチャが一致しません |
| ML0009 | MapFromメソッドのシグネチャが一致しません |
| ML0010 | MapFromMethodで指定されたメソッドは引数なしのインスタンスメソッドである必要があります |
| ML0011 | MapFromメソッドの戻り値の型がターゲットプロパティの型と一致しません |
| ML0012 | MapFromMethodメソッドの戻り値の型がターゲットプロパティの型と一致しません |

## 13. 実装ステータス

### 13.1 実装済み機能

| 機能 | ステータス |
|------|----------|
| 基本マッピング（同名プロパティ） | ✅ 実装済み |
| 異なるプロパティ名のマッピング | ✅ 実装済み |
| MapIgnore | ✅ 実装済み |
| MapConstant | ✅ 実装済み |
| MapConstant<T> (Generic) | ✅ 実装済み |
| BeforeMap / AfterMap | ✅ 実装済み |
| 型変換（数値、文字列、日時等） | ✅ 実装済み |
| Flatten（ネストソース→フラット） | ✅ 実装済み |
| Unflatten（フラット→ネストデスティネーション） | ✅ 実装済み |
| 多段ネスト対応 | ✅ 実装済み |
| Null安全処理 | ✅ 実装済み |
| カスタムパラメータ | ✅ 実装済み |
| Converter | ✅ 実装済み |
| 条件付きマッピング（グローバル） | ✅ 実装済み |
| 条件付きマッピング（プロパティレベル） | ✅ 実装済み |
| MapFrom（静的メソッドによる値の合成） | ✅ 実装済み |
| MapFromMethod（ソースオブジェクトのメソッド呼び出し） | ✅ 実装済み |
| AutoMap = false（自動マッピング無効化） | ✅ 実装済み |
| MapCollection（コレクションマッピング） | ✅ 実装済み |
| MapNested（子オブジェクトマッピング） | ✅ 実装済み |
| MapConverter（カスタム型変換器） | ✅ 実装済み |
| CollectionConverter（カスタムコレクション変換器） | ✅ 実装済み |

### 13.2 未実装機能

| 機能 | 説明 | 参考（AutoMapper相当） |
|------|------|----------------------|
| NullSubstitute | null値の代替値設定 | `NullSubstitute("N/A")` |

## 14. コレクションマッピング（MapCollection）

### 14.1 設計方針

- **自動マッピングは行わない**: コレクションプロパティが同名でも自動的にはマッピングしない
- **明示的なマッパーメソッド指定が必須**: 子要素のマッパーメソッドを先に定義し、それを使用する
- **コレクション変換はDefaultCollectionConverterを使用**: カスタムコレクションコンバーターで差し替え可能
- **voidマッパーとreturnマッパー両方対応**: 子要素マッパーはどちらのパターンでも使用可能

### 14.2 属性

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapCollectionAttribute : Attribute
{
    /// <summary>ソースプロパティ名</summary>
    public string Source { get; }
    
    /// <summary>ターゲットプロパティ名</summary>
    public string Target { get; }
    
    /// <summary>子要素のマッパーメソッド名（必須）</summary>
    public string MapperMethod { get; set; }
    
    public MapCollectionAttribute(string source, string target) { ... }
}
```

### 14.3 使用例

```csharp
public class SourceChild
{
    public int Value { get; set; }
}

public class Source
{
    public SourceChild[] Children { get; set; }
}

public class DestinationChild
{
    public int Value { get; set; }
}

public class Destination
{
    public List<DestinationChild> Children { get; set; }
}

internal static partial class ObjectMapper
{
    // 1. 子要素のマッパーを定義（必須）
    [Mapper]
    public static partial DestinationChild MapChild(SourceChild source);
    
    // 2. コレクションマッピングを指定
    [Mapper]
    [MapCollection(nameof(Source.Children), nameof(Destination.Children), MapperMethod = nameof(MapChild))]
    public static partial void Map(Source source, Destination destination);
}
```

**生成コード:**

```csharp
public static partial DestinationChild MapChild(SourceChild source)
{
    var destination = new DestinationChild();
    destination.Value = source.Value;
    return destination;
}

public static partial void Map(Source source, Destination destination)
{
    destination.Children = global::MapperLibrary.DefaultCollectionConverter.ToList<SourceChild, DestinationChild>(
        source.Children, MapChild)!;
}
```

### 14.4 コレクション変換器

コレクションの変換には `DefaultCollectionConverter` が使用されます。ターゲットの型に応じて `ToArray()` または `ToList()` が呼び出されます。

**生成コードのパターン:**

```csharp
// 配列の場合
destination.Items = DefaultCollectionConverter.ToArray<TSource, TDest>(source.Items, MapChild)!;

// Listの場合
destination.Items = DefaultCollectionConverter.ToList<TSource, TDest>(source.Items, MapChild)!;
```

### 14.5 voidマッパーの場合

voidマッパーの場合、`Action<TSource, TDest>` を受け取るオーバーロードが使用されます：

```csharp
[Mapper]
public static partial void MapChild(SourceChild source, DestinationChild destination);

[Mapper]
[MapCollection(nameof(Source.Children), nameof(Destination.Children), MapperMethod = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Children = global::MapperLibrary.DefaultCollectionConverter.ToList<SourceChild, DestinationChild>(
        source.Children, MapChild)!;
}
```

### 14.6 nullハンドリング

ソースコレクションがnullの場合、`DefaultCollectionConverter` が `default` を返し、null-forgiving演算子で `default!` が設定されます。

## 15. 子オブジェクトマッピング（MapNested）

### 15.1 設計方針

- **明示的なマッパーメソッド指定が必須**: コレクションと同様
- **voidマッパーとreturnマッパー両方対応**: 子オブジェクトマッパーはどちらのパターンでも使用可能
- **MapProperty / MapFrom との使い分け**:
  - `[MapProperty]`: 単純なプロパティコピー、またはドット記法によるネスト展開
  - `[MapFrom]`: 静的メソッドでソース全体から値を計算
  - `[MapNested]`: 別のマッパーメソッドを呼び出して子オブジェクトを変換

### 15.2 属性

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class MapNestedAttribute : Attribute
{
    /// <summary>ソースプロパティ名</summary>
    public string Source { get; }
    
    /// <summary>ターゲットプロパティ名</summary>
    public string Target { get; }
    
    /// <summary>子オブジェクトのマッパーメソッド名（必須）</summary>
    public string MapperMethod { get; set; }
    
    public MapNestedAttribute(string source, string target) { ... }
}
```

### 15.3 使用例

```csharp
public class SourceChild
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Source
{
    public SourceChild? Child { get; set; }
    public SourceChild[]? Children { get; set; }
}

public class DestinationChild
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class Destination
{
    public DestinationChild? Child { get; set; }
    public List<DestinationChild>? Children { get; set; }
}

internal static partial class ObjectMapper
{
    // 子要素のマッパーを定義
    [Mapper]
    public static partial DestinationChild MapChild(SourceChild source);
    
    // 子オブジェクトとコレクション両方を指定
    [Mapper]
    [MapNested(nameof(Source.Child), nameof(Destination.Child), MapperMethod = nameof(MapChild))]
    [MapCollection(nameof(Source.Children), nameof(Destination.Children), MapperMethod = nameof(MapChild))]
    public static partial void Map(Source source, Destination destination);
}
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    // 子オブジェクトマッピング
    destination.Child = source.Child is not null ? MapChild(source.Child!) : default!;
    
    // コレクションマッピング
    destination.Children = source.Children is not null 
        ? source.Children.Select(x => MapChild(x)).ToList() 
        : default!;
}
```

### 15.4 voidマッパーの場合

```csharp
[Mapper]
public static partial void MapChild(SourceChild source, DestinationChild destination);

[Mapper]
[MapNested(nameof(Source.Child), nameof(Destination.Child), MapperMethod = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Child = source.Child is not null 
        ? ((global::System.Func<DestinationChild>)(() => { 
            var __nested = new DestinationChild(); 
            MapChild(source.Child!, __nested); 
            return __nested; 
          }))() 
        : default!;
}
```

### 15.5 nullハンドリング

ソースの子オブジェクトがnullの場合、ターゲットには`default!`が設定されます（終端要素として扱い、通常のプロパティマッピングと同じルール）。

## 16. 属性の使い分けまとめ

| 属性 | 用途 | 例 |
|------|------|-----|
| `[MapProperty("A", "B")]` | 単純なプロパティコピー | `d.B = s.A` |
| `[MapProperty("A.X", "B")]` | ソースネストからフラットへ | `d.B = s.A.X` |
| `[MapProperty("A", "B.X")]` | フラットからターゲットネストへ | `d.B.X = s.A` |
| `[MapFrom("B", nameof(Method))]` | 静的メソッドでソース全体から計算 | `d.B = Method(s)` |
| `[MapFromMethod("B", "GetX")]` | ソースのメソッド呼び出し | `d.B = s.GetX()` |
| `[MapNested("A", "B", ...)]` | 別マッパーで子オブジェクト変換 | `d.B = MapChild(s.A)` |
| `[MapCollection("A", "B", ...)]` | 別マッパーでコレクション変換 | `DefaultCollectionConverter.ToList(...)` |

## 17. カスタム型変換器（MapConverter）

### 17.1 設計方針

- **Generic静的メソッドベース**: インスタンス生成のオーバーヘッドを回避
- **スペシャライズドメソッドの優先**: 特定の型変換に最適化されたメソッドを優先使用
- **JIT最適化**: `typeof(T) == typeof(int)` のような条件分岐はJITで最適化される
- **階層的な適用**: メソッドレベル → クラスレベル → デフォルトの優先順位

### 17.2 スペシャライズドメソッド

型変換時、ジェネレーターは以下の優先順位でメソッドを選択します：

1. **スペシャライズドメソッド** (存在する場合): `{MethodPrefix}To{TargetTypeName}(sourceType)`
2. **Genericメソッド** (フォールバック): `{MethodPrefix}<TSource, TDestination>(source)`

#### スペシャライズドメソッドの命名規則

```
{コンバーターメソッド名}To{ターゲット型名}
```

例：
- `ConvertToInt32(string source)` - `string` → `int` 変換
- `ConvertToString(int source)` - `int` → `string` 変換
- `ConvertToDateTime(string source)` - `string` → `DateTime` 変換

#### スペシャライズドメソッドの利点

1. **オーバーヘッド削減**: Genericメソッドの型チェック分岐を回避
2. **直接呼び出し**: JITコンパイル時の最適化が容易
3. **カルチャ制御**: `CultureInfo.InvariantCulture` などを直接指定可能

#### 例: DefaultValueConverterのスペシャライズドメソッド

```csharp
public static class DefaultValueConverter
{
    // スペシャライズドメソッド（優先使用される）
    public static int ConvertToInt32(string source)
        => int.Parse(source, CultureInfo.InvariantCulture);

    public static string ConvertToString(int source)
        => source.ToString(CultureInfo.InvariantCulture);

    public static DateTime ConvertToDateTime(string source)
        => DateTime.Parse(source, CultureInfo.InvariantCulture);

    // Genericフォールバック（スペシャライズドメソッドがない場合に使用）
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        // 型チェックによる変換
        // ...
    }
}
```

**生成コード例:**

```csharp
// スペシャライズドメソッドが存在する場合
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);

// スペシャライズドメソッドが存在しない場合（Genericフォールバック）
destination.DecimalValue = DefaultValueConverter.Convert<double, decimal>(source.DoubleValue);
```

### 17.3 カスタムコンバーターでのスペシャライズドメソッド

カスタムコンバーターでもスペシャライズドメソッドを定義できます：

```csharp
public static class CustomConverter
{
    // スペシャライズドメソッド: string -> int に1000を加算
    public static int ConvertToInt32(string source)
    {
        return int.Parse(source, CultureInfo.InvariantCulture) + 1000;
    }

    // スペシャライズドメソッド: int -> string に"PREFIX_"を追加
    public static string ConvertToString(int source)
    {
        return $"PREFIX_{source}";
    }

    // Genericフォールバック
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        return DefaultValueConverter.Convert<TSource, TDestination>(source);
    }
}

[Mapper]
[MapConverter(typeof(CustomConverter))]
public static partial void Map(Source source, Destination destination);
```

**生成コード:**

```csharp
// string -> int: スペシャライズドメソッド使用（+1000される）
destination.IntValue = CustomConverter.ConvertToInt32(source.StringValue);

// int -> string: スペシャライズドメソッド使用（PREFIX_付与）
destination.StringValue = CustomConverter.ConvertToString(source.IntValue);

// double -> decimal: Genericフォールバック使用
destination.DecimalValue = CustomConverter.Convert<double, decimal>(source.DoubleValue);
```

### 17.4 デフォルト実装（DefaultValueConverter）

型変換が必要な場合、`DefaultValueConverter` が使用されます：

```csharp
public static class DefaultValueConverter
{
    // スペシャライズドメソッド群
    public static int ConvertToInt32(string source) => int.Parse(source, CultureInfo.InvariantCulture);
    public static long ConvertToInt64(string source) => long.Parse(source, CultureInfo.InvariantCulture);
    public static string ConvertToString(int source) => source.ToString(CultureInfo.InvariantCulture);
    public static string ConvertToString(long source) => source.ToString(CultureInfo.InvariantCulture);
    // ... 他の型のスペシャライズドメソッド

    // Genericフォールバック
    // Genericフォールバック（スペシャライズドメソッドがない場合に使用）
    // 注意: Nullable処理はSource Generator側で行われる
    //       このメソッドには常に非Nullable型が渡される
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        // 数値変換（int, long, double, etc.）
        // string <-> 数値/bool/DateTime/Guid
        // フォールバック処理
        // ...
    }
}
```

### 17.5 コンバーターが使用されるケース

**コンバーターが使用される（型変換が必要）:**

| ソース型 | ターゲット型 | 生成コード |
|----------|-------------|-----------|
| `string` | `int` | `ConvertToInt32(source.Value)` |
| `int` | `string` | `ConvertToString(source.Value)` |
| `int?` | `string` | `source.Value is not null ? ConvertToString(source.Value.Value) : default!` |
| `long` | `int` | `Convert<long, int>(source.Value)` |

**コンバーターが使用されない（直接代入）:**

| ソース型 | ターゲット型 | 生成コード |
|----------|-------------|-----------|
| `int` | `int` | `destination.Prop = source.Prop;` |
| `int` | `long` | `destination.Prop = source.Prop;` (暗黙的変換) |
| `int` | `int?` | `destination.Prop = source.Prop;` |
| `int?` | `int` | `destination.Prop = source.Prop!;` (null-forgiving) |

### 17.6 Nullable型の変換処理

Nullable型の処理はSource Generator側で行われ、コンバーターにはアンダーライング型（非Nullable）が渡されます。

**例: int? → string の変換**

```csharp
// ソースコード
public class Source { public int? NullableValue { get; set; } }
public class Destination { public string StringValue { get; set; } }

// 生成されるコード
destination.StringValue = source.NullableValue is not null
    ? DefaultValueConverter.ConvertToString(source.NullableValue.Value)  // int -> string
    : default!;
```

**NullBehavior.Skip の場合:**

```csharp
// NullBehavior = NullBehavior.Skip が指定された場合
if (source.NullableValue is not null)
{
    destination.StringValue = DefaultValueConverter.ConvertToString(source.NullableValue.Value);
}
```

### 17.7 カスタムコンバーターの指定

#### 指定レベルと優先順位

| レベル | 適用範囲 | 優先順位 |
|--------|----------|----------|
| MapPropertyのConverterプロパティ | 指定したプロパティのみ | 最高 |
| Mapperメソッドの`[MapConverter]` | そのメソッドの全プロパティ | 中 |
| クラスの`[MapConverter]` | クラス内の全Mapperメソッド | 低 |
| デフォルト | DefaultValueConverter | 最低 |

#### 属性

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class MapConverterAttribute : Attribute
{
    /// <summary>
    /// コンバーター型。以下のシグネチャの静的メソッドが必要：
    /// - スペシャライズド: {Method}To{TargetType}(sourceType) （任意）
    /// - Generic: TDestination {Method}&lt;TSource, TDestination&gt;(TSource source) （必須）
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// メソッド名のプレフィックス。デフォルトは "Convert"。
    /// スペシャライズドメソッドは "{Method}To{TargetType}" の形式で検索されます。
    /// </summary>
    public string Method { get; set; } = "Convert";

    public MapConverterAttribute(Type converterType) { ... }
}
```

### 17.8 使用例

#### プロパティレベルの指定（MapPropertyのConverterプロパティ）

```csharp
[Mapper]
[MapProperty(nameof(Destination.FormattedValue), nameof(Source.Value), Converter = nameof(FormatValue))]
public static partial void Map(Source source, Destination destination);

private static string FormatValue(int value)
{
    return $"Value: {value}";
}
```

#### メソッドレベルの指定

```csharp
// カスタムコンバーターの実装
public static class CustomConverter
{
    // スペシャライズドメソッド（優先使用）
    public static string ConvertToString(int source) => $"ID_{source}";

    // Genericフォールバック
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        return DefaultValueConverter.Convert<TSource, TDestination>(source);
    }
}

// メソッドレベルで指定（このメソッドの全プロパティに適用）
[Mapper]
[MapConverter(typeof(CustomConverter))]
public static partial void Map(Source source, Destination destination);
```

#### クラスレベルの指定

```csharp
// クラスレベルで指定（すべてのマッパーに適用）
[MapConverter(typeof(CustomConverter))]
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);
    
    // このメソッドはメソッドレベルの指定が優先される
    [Mapper]
    [MapConverter(typeof(SpecialConverter))]
    public static partial void MapSpecial(Source source, Destination destination);
}
```

## 18. カスタムコレクション変換器（CollectionConverter）

### 18.1 設計方針

- **コレクション変換の統一**: すべてのコレクションマッピングは `DefaultCollectionConverter` を通じて行われる
- **カスタマイズ可能**: `CollectionConverterAttribute` で差し替え可能

### 18.2 デフォルト実装（DefaultCollectionConverter）

```csharp
public static class DefaultCollectionConverter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        return source.Select(mapper).ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        return source.Select(mapper).ToList();
    }

    // voidマッパー用のオーバーロード
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper) where TDest : new() { ... }

    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper) where TDest : new() { ... }
}
```

### 18.3 カスタムコレクションコンバーターの指定

#### 属性

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public sealed class CollectionConverterAttribute : Attribute
{
    /// <summary>
    /// コンバーター型。以下のシグネチャの静的メソッドが必要：
    /// TDest[]? ToArray&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Func&lt;TSource, TDest&gt; mapper)
    /// List&lt;TDest&gt;? ToList&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Func&lt;TSource, TDest&gt; mapper)
    /// </summary>
    public Type ConverterType { get; }
    
    public CollectionConverterAttribute(Type converterType) { ... }
}
```

#### 使用例

```csharp
// カスタムコレクションコンバーターの実装
public static class CustomCollectionConverter
{
    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        // カスタム処理（例：変換後にソート）
        return source.Select(mapper).OrderBy(x => x).ToList();
    }

    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null) return default;
        return source.Select(mapper).OrderBy(x => x).ToArray();
    }
}

// 使用
[Mapper]
[CollectionConverter(typeof(CustomCollectionConverter))]
[MapCollection(nameof(Source.Items), nameof(Destination.Items), MapperMethod = nameof(MapItem))]
public static partial void Map(Source source, Destination destination);
```

## 19. 未実装機能

| 機能 | 説明 |
|------|------|
| NullSubstitute | null値の代替値設定 |

