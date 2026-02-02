# MapperGenerator アーキテクチャドキュメント

## 概要

`MapperGenerator` は Incremental Source Generator として実装された、Smart.Mapper のコード生成の中核をなすクラスです。
`[Mapper]` 属性が付与された partial メソッドを検出し、プロパティマッピングを行う実装コードを生成します。

**ファイル位置**: `Smart.Mapper.Generator/Mapper/Generator/MapperGenerator.cs`

---

## メソッド一覧

### 初期化

| メソッド | 説明 |
|---------|------|
| `Initialize` | Incremental Generator のエントリポイント。`[Mapper]` 属性を検出し、パイプラインを構成 |

### 解析フェーズ（Parser）

| メソッド | 説明 |
|---------|------|
| `IsMethodSyntax` | 対象がメソッド構文かどうかを判定 |
| `GetMapperMethodModel` | `[Mapper]` 属性が付いたメソッドを解析し、`MapperMethodModel` を構築 |
| `ParseMappingAttributes` | マッピング関連の属性を解析（`[MapProperty]`, `[MapIgnore]`, `[MapConstant]` など） |
| `ParseConverterAttributes` | コンバーター関連の属性を解析（`[MapConverter]`, `[CollectionConverter]`） |

### バリデーション

| メソッド | 説明 |
|---------|------|
| `ValidateConverterMethods` | プロパティレベルのコンバーターメソッドの妥当性を検証 |
| `ValidatePropertyConditionMethods` | プロパティ条件メソッドの妥当性を検証 |
| `ValidateCallbackMethods` | `[BeforeMap]`, `[AfterMap]` コールバックメソッドの妥当性を検証 |
| `ValidateConditionMethod` | グローバル条件メソッドの妥当性を検証 |
| `ValidateAndBuildMapUsingMappings` | `[MapUsing]` マッピングの検証とビルド |
| `ValidateAndBuildMapFromMappings` | `[MapFrom]` マッピングの検証とビルド |
| `ValidateAndBuildMapCollectionMappings` | `[MapCollection]` マッピングの検証とビルド |
| `ValidateAndBuildMapNestedMappings` | `[MapNested]` マッピングの検証とビルド |

### ヘルパー（解析）

| メソッド | 説明 |
|---------|------|
| `FindMatchingCallbackMethod` | コールバックメソッドの候補からシグネチャが一致するものを検索 |
| `FindMatchingMapUsingMethod` | `[MapUsing]` 用のメソッドを検索 |
| `FindMapperMethod` | `[MapCollection]`, `[MapNested]` 用のマッパーメソッドを検索 |
| `IsAssignableTo` | 型の代入可能性を判定 |
| `ResolvePropertyPath` | プロパティパス（例: `"Child.Value"`）の型を解決 |
| `GetCollectionElementType` | コレクション型から要素型を取得 |
| `FormatConstantValue` | 定数値をコード文字列にフォーマット |

### プロパティマッピング構築

| メソッド | 説明 |
|---------|------|
| `BuildPropertyMappings` | ソース/デスティネーション型からプロパティマッピングを構築 |
| `BuildConstantMappings` | 定数マッピング（`[MapConstant]`）の型情報を構築 |
| `ResolveNestedMapping` | ネストしたプロパティパスの型情報を解決 |
| `GetUnderlyingType` | Nullable 型のアンダーライング型を取得 |
| `IsNullableSymbol` | 型が Nullable かどうかを判定 |
| `RequiresTypeConversion` | 型変換が必要かどうかを判定 |
| `IsImplicitNumericConversion` | 暗黙的な数値変換が可能かを判定 |
| `NormalizeTypeName` | 型名を正規化（`global::System.Int32` → `Int32`） |
| `ResolvePropertyType` | プロパティパスから型を解決 |
| `GetAllProperties` | 型のすべてのプロパティを取得（継承を含む） |

### コンバーター検出

| メソッド | 説明 |
|---------|------|
| `DetectSpecializedConverterMethods` | スペシャライズドコンバーターメソッドを検出 |
| `FindConverterType` | コンバーター型を検索 |
| `FindSpecializedMethod` | スペシャライズドメソッド（例: `ConvertToInt32`）を検索 |
| `GetSimpleTypeName` | 完全修飾型名から単純型名を取得 |
| `ExtractLastSegment` | 型名の最後のセグメントを抽出 |

### コード生成フェーズ（Emitter）

| メソッド | 説明 |
|---------|------|
| `Execute` | ソース生成のエントリポイント |
| `BuildSource` | クラス全体のソースコードを構築 |
| `BuildMethod` | 個別のマッパーメソッドのコードを生成 |
| `BuildPropertyAssignment` | プロパティ代入文を生成（メインのディスパッチャー） |
| `BuildStandardAssignment` | 標準的なプロパティ代入を生成 |
| `BuildNullableSourceConversion` | Nullable ソースからの型変換コードを生成 |
| `BuildTypeConversion` | 型変換コードを生成 |
| `BuildTypeConversionWithValueAccess` | `.Value` アクセス付きの型変換コードを生成 |
| `GetNullCheckCondition` | null チェック条件式を生成 |
| `BuildSourceAccessor` | ソースアクセサー式を生成（例: `source.Child.Value`） |

### ユーティリティ

| メソッド | 説明 |
|---------|------|
| `MakeFilename` | 生成ファイル名を作成 |

---

## メソッド呼び出し関係

```
Initialize
    └── GetMapperMethodModel ─────────────────────┐
            ├── IsMethodSyntax                     │
            ├── ParseMappingAttributes             │
            │       └── (属性のパース)             │
            ├── ParseConverterAttributes           │
            ├── BuildPropertyMappings              │
            │       ├── GetAllProperties           │
            │       ├── IsNullableSymbol           │
            │       ├── GetUnderlyingType          │
            │       └── RequiresTypeConversion     │
            │               └── IsImplicitNumericConversion
            │               └── NormalizeTypeName  │
            ├── DetectSpecializedConverterMethods  │
            │       ├── FindConverterType          │
            │       ├── FindSpecializedMethod      │
            │       └── GetSimpleTypeName          │
            │               └── ExtractLastSegment │
            ├── ValidateConverterMethods           │
            ├── ValidatePropertyConditionMethods   │
            ├── ValidateCallbackMethods            │
            │       └── FindMatchingCallbackMethod │
            ├── ValidateConditionMethod            │
            ├── ValidateAndBuildMapUsingMappings   │
            │       └── FindMatchingMapUsingMethod │
            ├── ValidateAndBuildMapFromMappings    │
            │       └── ResolvePropertyPath        │
            ├── ValidateAndBuildMapCollectionMappings
            │       ├── GetCollectionElementType   │
            │       └── FindMapperMethod           │
            ├── ValidateAndBuildMapNestedMappings  │
            │       └── FindMapperMethod           │
            ├── BuildConstantMappings              │
            │       └── FormatConstantValue        │
            └── ResolveNestedMapping               │
                    └── IsNullableSymbol           │
                                                   │
Execute ◄──────────────────────────────────────────┘
    └── BuildSource
            └── BuildMethod
                    ├── BuildPropertyAssignment
                    │       ├── BuildStandardAssignment
                    │       │       ├── BuildSourceAccessor
                    │       │       └── BuildTypeConversion
                    │       └── BuildNullableSourceConversion
                    │               └── BuildTypeConversionWithValueAccess
                    └── GetNullCheckCondition
```

---

## 各メソッドの処理詳細

### 初期化フェーズ

#### `Initialize(IncrementalGeneratorInitializationContext context)`

Incremental Source Generator のエントリポイント。

**処理内容**:
1. `ForAttributeWithMetadataName` で `[Mapper]` 属性が付いたシンボルを収集
2. `GetMapperMethodModel` で各メソッドを解析
3. `Execute` でソースコードを生成

---

### 解析フェーズ

#### `GetMapperMethodModel(GeneratorAttributeSyntaxContext context)`

`[Mapper]` 属性が付いたメソッドを解析し、`MapperMethodModel` を構築する。

**処理フロー**:
1. メソッドシンボルの取得と基本情報の抽出
2. パラメータの検証（ソース、デスティネーション、カスタムパラメータ）
3. 属性の解析（`ParseMappingAttributes`, `ParseConverterAttributes`）
4. プロパティマッピングの構築（`BuildPropertyMappings`）
5. スペシャライズドコンバーターの検出
6. 各種バリデーション
7. 特殊マッピングの検証とビルド（MapUsing, MapFrom, MapCollection, MapNested）
8. 定数マッピングの構築

**戻り値**: `Result<MapperMethodModel>` - 成功時はモデル、失敗時はエラー情報

---

#### `ParseMappingAttributes(IMethodSymbol symbol, MapperMethodModel model)`

マッピング関連の属性を解析する。

**対応属性**:
- `[MapProperty]` - プロパティマッピング
- `[MapIgnore]` - マッピング除外
- `[MapConstant]` - 定数マッピング
- `[MapExpression]` - 式によるマッピング
- `[MapUsing]` - カスタムメソッドによるマッピング
- `[MapFrom]` - ソースパス/メソッドによるマッピング
- `[MapCollection]` - コレクションマッピング
- `[MapNested]` - ネストオブジェクトマッピング
- `[BeforeMap]` / `[AfterMap]` - コールバック
- `[MapCondition]` - グローバル条件

---

#### `BuildPropertyMappings(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodModel model)`

ソース型とデスティネーション型からプロパティマッピングを構築する。

**処理内容**:
1. ソースとデスティネーションの全プロパティを取得
2. カスタムマッピング（`[MapProperty]`）とネストマッピングを分離
3. デスティネーションプロパティをイテレート:
   - 無視プロパティをスキップ
   - カスタムマッピングがあれば適用
   - なければ同名プロパティを自動マッピング（`AutoMap = true` の場合）
4. 型情報（Nullable、アンダーライング型）を設定
5. 型変換の必要性を判定
6. ネストマッピングを追加

---

#### `DetectSpecializedConverterMethods(MapperMethodModel model, IMethodSymbol mapperMethod)`

スペシャライズドコンバーターメソッドを検出する。

**検出ルール**:
- メソッド名: `{MethodPrefix}To{TargetTypeName}` （例: `ConvertToInt32`）
- 引数: ソース型と一致
- 戻り値: ターゲット型と一致

**処理フロー**:
1. コンバーター型を取得（カスタムまたはデフォルト）
2. 各マッピングに対してスペシャライズドメソッドを検索
3. 見つかった場合、`SpecializedConverterMethod` を設定

---

### バリデーションフェーズ

#### `ValidateAndBuildMapCollectionMappings(...)`

`[MapCollection]` マッピングを検証しビルドする。

**検証内容**:
1. ソース/デスティネーションプロパティの存在
2. コレクション型であること
3. マッパーメソッドの存在とシグネチャ

**設定される情報**:
- `SourceType`, `TargetType` - コレクション型
- `SourceElementType`, `TargetElementType` - 要素型
- `MapperReturnsValue` - マッパーが値を返すかどうか
- `TargetIsArray` - ターゲットが配列かどうか
- `Converter` - カスタムコンバーターメソッド名

---

### コード生成フェーズ

#### `BuildMethod(SourceBuilder builder, MapperMethodModel method)`

個別のマッパーメソッドのコードを生成する。

**生成される要素**:
1. メソッドシグネチャ
2. デスティネーション変数の初期化（返り値がある場合）
3. `BeforeMap` コールバック呼び出し
4. ネストパスの自動インスタンス化
5. プロパティマッピング（null チェックでグループ化）
6. 定数マッピング
7. 式マッピング
8. MapUsing マッピング
9. MapFrom マッピング
10. MapNested マッピング
11. MapCollection マッピング
12. `AfterMap` コールバック呼び出し
13. return 文（返り値がある場合）

---

#### `BuildPropertyAssignment(SourceBuilder builder, PropertyMappingModel mapping, ...)`

プロパティ代入文を生成するメインのディスパッチャー。

**処理フロー**:
1. プロパティレベルの条件チェック（`HasCondition`）
2. Nullable ソース + 型変換が必要な場合:
   - `BuildNullableSourceConversion` を呼び出し
3. それ以外:
   - `BuildStandardAssignment` を呼び出し

---

#### `BuildNullableSourceConversion(...)`

Nullable ソースからの型変換コードを生成する。

**生成パターン**:

**NullBehavior.Skip の場合**:
```csharp
if (source.Prop is not null)
{
    destination.Prop = Converter.ConvertToXxx(source.Prop.Value);
}
```

**NullBehavior.Default の場合**:
```csharp
destination.Prop = source.Prop is not null
    ? Converter.ConvertToXxx(source.Prop.Value)
    : default!;
```

---

#### `BuildTypeConversion(SourceBuilder builder, PropertyMappingModel mapping, ...)`

型変換コードを生成する。

**生成パターン**:

**スペシャライズドメソッドがある場合**:
```csharp
Converter.ConvertToInt32(source.Value)
```

**スペシャライズドメソッドがない場合**:
```csharp
Converter.Convert<int, string>(source.Value)
```

---

## データモデル

### MapperMethodModel

マッパーメソッド全体の情報を保持。

```csharp
class MapperMethodModel
{
    string MethodName;                           // メソッド名
    string SourceTypeName;                       // ソース型
    string DestinationTypeName;                  // デスティネーション型
    bool ReturnsDestination;                     // 戻り値があるか
    bool AutoMap;                                // 自動マッピング有効か
    List<PropertyMappingModel> PropertyMappings; // プロパティマッピング
    List<ConstantMappingModel> ConstantMappings; // 定数マッピング
    List<MapUsingModel> MapUsingMappings;        // MapUsing マッピング
    List<MapFromModel> MapFromMappings;          // MapFrom マッピング
    List<MapCollectionModel> MapCollectionMappings; // MapCollection マッピング
    List<MapNestedModel> MapNestedMappings;      // MapNested マッピング
    string? MapConverterTypeName;                // カスタムコンバーター型
    string? CollectionConverterTypeName;         // カスタムコレクションコンバーター型
    // ... 他のプロパティ
}
```

### PropertyMappingModel

プロパティマッピングの情報を保持。

```csharp
class PropertyMappingModel
{
    string SourcePath;                  // ソースプロパティパス
    string TargetPath;                  // ターゲットプロパティパス
    string SourceType;                  // ソース型（Nullable 含む）
    string TargetType;                  // ターゲット型（Nullable 含む）
    string SourceUnderlyingType;        // ソースのアンダーライング型
    string TargetUnderlyingType;        // ターゲットのアンダーライング型
    bool RequiresConversion;            // 型変換が必要か
    bool IsSourceNullable;              // ソースが Nullable か
    bool IsTargetNullable;              // ターゲットが Nullable か
    string? SpecializedConverterMethod; // スペシャライズドコンバーターメソッド名
    NullBehaviorType NullBehavior;      // null 時の動作
    int Order;                          // 実行順序
}
```

---

## 生成コード例

### 入力

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]
[MapCollection(nameof(Destination.Items), nameof(Source.Items), Mapper = nameof(MapItem))]
public static partial Destination Map(Source source);
```

### 出力

```csharp
public static partial Destination Map(Source source)
{
    var __destination = new Destination();
    __destination.Id = source.Id;
    __destination.FullName = source.Name;
    __destination.Items = global::Smart.Mapper.DefaultCollectionConverter.ToList<SourceItem, DestItem>(source.Items, MapItem)!;
    return __destination;
}
```
