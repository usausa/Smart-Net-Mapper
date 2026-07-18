# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** is a high-performance object mapper library based on Roslyn Incremental Source Generator.
It automatically generates property-copying code at compile time for `static partial` methods decorated with the `[Mapper]` attribute.

## Features

- **Zero overhead** - no reflection at runtime; all code is generated statically at compile time
- **Specialized-method dispatch** - `ConvertTo{TargetType}` naming convention enables direct-call generation, friendly to JIT inlining
- **Per-method declaration** - `[Mapper]` is placed on individual methods, so mapper methods feel like ordinary helper functions
- **Custom parameter passthrough** - additional arguments such as `Map(Src, Dst, TContext ctx)` are transparently propagated to all hooks
- **NativeAOT / trimming fully supported** - `<IsAotCompatible>true</IsAotCompatible>` declared; NativeAOT smoke test passes
- **Rich diagnostics** - 28 compile-time diagnostics in phase-based bands (SMP0001–SMP0501)

## Installation

```
dotnet add package Usa.Smart.Mapper
```

The package includes the source generator DLL under `analyzers/dotnet/cs`, so the generator is activated automatically when you reference the package - no additional setup required.

## Target Frameworks

| Library | Frameworks |
|---------|-----------|
| `Smart.Mapper` | net10.0, net9.0, net8.0 |
| `Smart.Mapper.Generator` | netstandard2.0 (Roslyn Incremental Source Generator) |

---

## Quick Start

```csharp
// Define mapper in a static partial class
internal static partial class ObjectMapper
{
    // void pattern: map into an existing instance
    [Mapper]
    public static partial void Map(Source source, Destination destination);

    // return pattern: create and return a new instance
    [Mapper]
    public static partial Destination Map(Source source);
}
```

### Generated code (void pattern)

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
}
```

### Generated code (return pattern)

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

## Attribute Reference

### Method-level attributes

| Attribute | Description |
|-----------|-------------|
| `[Mapper]` | Marks a method as a mapping method |
| `[Mapper(AutoMap = false)]` | Disables automatic same-name property mapping |
| `[Mapper(Strict = true)]` | Emits SMP0501 warning for unmapped destination properties |
| `[Mapper(NameComparison = ...)]` | Property name comparison mode for auto-mapping (default: `Ordinal`) |
| `[Mapper(Culture = "...")]` | Culture used for type conversion (e.g., `"ja-JP"`) |
| `[Mapper(DateTimeFormat = "...")]` | Format string for `DateTime` <-> `string` conversion (use with `Culture`) |
| `[Mapper(NumberFormat = "...")]` | Format string for numeric <-> `string` conversion (use with `Culture`) |
| `[MapProperty]` | Explicit property-to-property mapping; supports `NullSubstitute`, `Culture`, `DateTimeFormat`, `NumberFormat`, `Converter` |
| `[MapProperty<T>]` | Type-safe variant of `[MapProperty]` (C# 11+) |
| `[MapUsing]` | Calculates a value via a static method (custom-parameter aware) |
| `[MapFrom]` | Maps from a source instance-method call or dot-notation property path |
| `[MapConstant]` | Sets a constant value on a destination property |
| `[MapConstant<T>]` | Type-safe variant of `[MapConstant]` (C# 11+) |
| `[MapExpression]` | Embeds an arbitrary C# expression (e.g., `"System.DateTime.Now"`) |
| `[MapIgnore]` | Excludes a destination property from mapping |
| `[BeforeMap]` | Callback invoked before mapping |
| `[AfterMap]` | Callback invoked after mapping |
| `[MapCondition]` | Conditional mapping - global or per-property |
| `[MapCollection]` | Collection property mapping via an explicit mapper method |
| `[MapNested]` | Nested object mapping via an explicit mapper method |
| `[ValueConverter]` | Custom type converter (method / class level) |
| `[CollectionConverter]` | Custom collection converter (method / class level) |

> **First argument convention** - For all attributes, the **first** argument is the **destination** (target) property name; the **second** is the source.

### Class-level attributes

| Attribute | Description |
|-----------|-------------|
| `[MapperProfile]` | Sets defaults (`Strict`, `NameComparison`, `Culture`, `DateTimeFormat`, `NumberFormat`) for all `[Mapper]` methods in the class; method-level settings take precedence |
| `[ValueConverter]` | Default custom type converter for all `[Mapper]` methods in the class |
| `[CollectionConverter]` | Default custom collection converter for all `[Mapper]` methods in the class |

---

## Core Features

### Auto-mapping

Same-name, compatible-type properties are mapped automatically.

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);
```

### Property remapping (`[MapProperty]`)

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]
public static partial void Map(Source source, Destination destination);
```

### Null substitution

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullSubstitute = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullSubstitute = 0)]
public static partial void Map(Source source, Destination destination);
```

### Ignore properties (`[MapIgnore]`)

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

### Static method calculation (`[MapUsing]`)

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

Custom parameters are automatically forwarded:

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

### Source method / property-path (`[MapFrom]`)

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // instance method call
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // dot-notation path
public static partial void Map(Source source, Destination destination);
```

### Constant values (`[MapConstant]` / `[MapConstant<T>]`)

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

Non-generic variant: `[MapConstant("Status", "Active")]`
For expressions: `[MapExpression("CreatedAt", "System.DateTime.Now")]`

### Before / After Map callbacks

```csharp
[Mapper]
[BeforeMap(nameof(BeforeMapping))]
[AfterMap(nameof(AfterMapping))]
public static partial void Map(Source source, Destination destination);

private static void BeforeMapping(Source source, Destination destination) { /* ... */ }
private static void AfterMapping(Source source, Destination destination) { /* ... */ }
```

### Conditional mapping (`[MapCondition]`)

Global condition:

```csharp
[Mapper]
[MapCondition(nameof(ShouldMap))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMap(Source source, Destination destination) => source.IsActive;
```

Per-property condition:

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

### Auto-mapping disabled (`AutoMap = false`)

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Source.Id), nameof(Destination.Id))]
public static partial void Map(Source source, Destination destination);
// Only 'Id' is mapped; other properties are ignored.
```

---

## Nested Property Mapping

Use dot notation in `[MapProperty]` to flatten or unflatten nested properties.

### Flatten (nested source -> flat destination)

```csharp
[Mapper]
[MapProperty("Child.Id",   "ChildId")]
[MapProperty("Child.Name", "ChildName")]
public static partial void Map(Source source, Destination destination);
```

Generated code adds a null guard for nullable intermediate objects:

```csharp
if (source.Child is not null)
{
    destination.ChildId   = source.Child.Id;
    destination.ChildName = source.Child.Name;
}
```

### Unflatten (flat source -> nested destination)

```csharp
[Mapper]
[MapProperty("Value1", "Child1.Value")]
[MapProperty("Value2", "Child2.Value")]
public static partial void Map(Source source, Destination destination);
```

Intermediate destination objects are auto-instantiated:

```csharp
destination.Child1 ??= new DestinationChild();
destination.Child2 ??= new DestinationChild();
destination.Child1.Value = source.Value1;
destination.Child2.Value = source.Value2;
```

---

## Collection Mapping (`[MapCollection]`)

An explicit mapper method must be specified.

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

Generated code:

```csharp
destination.Children = global::Smart.Mapper.DefaultCollectionConverter.ToList<SourceChild, DestinationChild>(
    source.Children, MapChild)!;
```

`DefaultCollectionConverter` provides `ToArray` / `ToList` overloads for both function-mapper and action-mapper variants. A null source collection returns `default`.

---

## Nested Object Mapping (`[MapNested]`)

```csharp
[Mapper]
[MapNested(nameof(Destination.Child), nameof(Source.Child), Mapper = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

Generated code:

```csharp
destination.Child = source.Child is not null ? MapChild(source.Child!) : default!;
```

---

## record / Primary Constructor Support

When the destination type is a `record` or has a primary constructor, the generator automatically uses constructor-call syntax.

```csharp
public record DestModel(int Id, string Name);

[Mapper]
public static partial DestModel Map(SrcModel src);
```

Generated code:

```csharp
public static partial DestModel Map(SrcModel src)
{
    var destination = new DestModel(src.Id, src.Name);
    return destination;
}
```

> `void` mapper is not allowed for `init-only` / `record` destination types (SMP0302).

---

## Null Handling

| Source type | Destination type | Behavior |
|-------------|-----------------|----------|
| `T?` | `T?` | Copied as-is (including null) |
| `T?` | `T` (leaf) | `default!` assigned when null |
| `T` | `T?` | Copied as-is |
| `T` | `T` | Copied as-is |

Nullable intermediate paths on the **source side** are guarded with `if (... is not null)`.
Nullable intermediate paths on the **destination side** are auto-instantiated with `??= new`.

---

## Type Conversion

Same-type and implicitly convertible assignments are generated without a converter.
When explicit conversion is needed, `DefaultValueConverter` is used.

### Specialized-method pattern

```csharp
// string -> int
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);

// int -> string
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);
```

### Nullable handling (handled by the generator)

```csharp
// int? -> string
destination.StringValue = source.NullableValue is not null
    ? DefaultValueConverter.ConvertToString(source.NullableValue.Value)
    : default!;
```

### Custom value converter (`[ValueConverter]`)

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

Priority order (highest to lowest):

| Level | Scope |
|-------|-------|
| `[MapProperty(Converter = nameof(...))]` | Single property |
| `[ValueConverter]` on mapper method | All properties of that method |
| `[ValueConverter]` on class | All mapper methods in the class |
| `DefaultValueConverter` | Fallback |

### Custom collection converter (`[CollectionConverter]`)

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

Priority: `[MapProperty]` > `[Mapper]` > `[MapperProfile]` > `CultureInfo.InvariantCulture`

The resolved `CultureInfo` is cached as a `static readonly` field in the generated class to avoid repeated `GetCultureInfo(...)` calls.

> Specifying `DateTimeFormat` / `NumberFormat` without `Culture` is a compile-time error (SMP0401).

---

## NativeAOT / Trimming

Smart.Mapper is fully compatible with NativeAOT and IL trimming.

- `<IsAotCompatible>true</IsAotCompatible>` is declared in `Smart.Mapper.csproj`
- All type conversions are handled through specialized methods - no generic reflection fallback at runtime
- `Activator.CreateInstance` is never used; object creation is expanded inline by the generator
- `[DynamicallyAccessedMembers]` annotations are applied to `ValueConverterAttribute.ConverterType` and `CollectionConverterAttribute.ConverterType`

> **`[MapExpression]` warning** - If an expression contains reflection APIs (`Activator`, `Type.GetType`, `MethodInfo`, etc.), SMP0403 is emitted. Prefer `[MapFrom]` or `[MapUsing]` in AOT contexts.

---

## Diagnostics

| Code | Description | Severity |
|------|-------------|----------|
| SMP0001 | Mapper method must be `static partial` | Error |
| SMP0002 | Invalid parameter count on mapper method | Error |
| SMP0003 | Duplicate custom parameter type | Error |
| SMP0101 | Duplicate mapping to the same destination property | Error |
| SMP0102 | `BeforeMap` method signature mismatch | Error |
| SMP0103 | `AfterMap` method signature mismatch | Error |
| SMP0104 | Converter method signature mismatch | Error |
| SMP0105 | Converter return type does not match destination property type | Error |
| SMP0106 | Property-condition method signature mismatch | Error |
| SMP0201 | `MapUsing` static method signature mismatch | Error |
| SMP0202 | `MapUsing` static method return type does not match destination property type | Error |
| SMP0203 | `[MapFrom]` target property not found on destination type | Error |
| SMP0204 | `MapFrom` member is not a valid parameterless instance method or property path on the source type | Error |
| SMP0205 | `MapFrom` member type does not match destination property type | Error |
| SMP0206 | `[MapCollection]` / `[MapNested]` source property not found on source type | Error |
| SMP0207 | `[MapCollection]` / `[MapNested]` target property not found on destination type | Error |
| SMP0208 | `[MapCollection]` source property is not a collection type | Error |
| SMP0209 | `[MapCollection]` target property is not a collection type | Error |
| SMP0210 | `MapCollection` element mapper method not found or signature mismatch | Error |
| SMP0211 | `MapNested` mapper method not found or signature mismatch | Error |
| SMP0212 | `[MapCollection]` / `[MapNested]` cannot target an init-only / required member | Error |
| SMP0301 | Constructor parameter has no matching source property | Error |
| SMP0302 | `void` mapper cannot be used with `init-only` / `record` destination | Error |
| SMP0303 | `required` member is not mapped | Error |
| SMP0401 | `DateTimeFormat` / `NumberFormat` specified without `Culture` | Error |
| SMP0402 | AOT incompatible: generic fallback `Convert<TSource,TDest>` is reachable | Error |
| SMP0403 | AOT warning: possible reflection pattern in `MapExpression` | Warning |
| SMP0501 | Strict mode: unmapped destination property | Warning |

---

## Benchmark

Measured with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) on .NET 10.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.300
  [Host] / MediumRun : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
Job=MediumRun  IterationCount=15  LaunchCount=2  WarmupCount=10
```

### Simple mapping

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 9.391 ns | 0.478 ns | 0.715 ns | 1.01 | 64 B |
| SmartMapper | 9.171 ns | 0.361 ns | 0.529 ns | 0.98 | 64 B |

### Type conversion mapping

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 93.17 ns | 4.364 ns | 6.531 ns | 1.00 | 128 B |
| SmartMapper | 88.73 ns | 3.195 ns | 4.782 ns | 0.96 | 128 B |

### Nested mapping

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 11.08 ns | 0.390 ns | 0.584 ns | 1.00 | 72 B |
| SmartMapper | 13.68 ns | 1.184 ns | 1.772 ns | 1.24 | 72 B |

### Void nested mapping (lambda elimination)

| Method | Mean | Error | StdDev | Ratio | Allocated |
|--------|-----:|------:|-------:|------:|----------:|
| Direct | 9.038 ns | 0.154 ns | 0.231 ns | 1.00 | 72 B |
| LegacyLambda | 9.341 ns | 0.283 ns | 0.424 ns | 1.03 | 72 B |
| SmartMapper | 9.160 ns | 0.395 ns | 0.591 ns | 1.01 | 72 B |

### Collection mapping — item level (both return `List<T>`)

Caller manages list; SmartMapper is used only for per-element mapping.

| Method | ItemCount | Mean | Error | StdDev | Ratio | Allocated |
|--------|----------:|-----:|------:|-------:|------:|----------:|
| Direct | 10 | 101.1 ns | 2.02 ns | 3.98 ns | 1.00 | 456 B |
| SmartMapper | 10 | 107.6 ns | 2.22 ns | 6.40 ns | 1.07 | 456 B |
| Direct | 100 | 812.7 ns | 29.30 ns | 86.40 ns | 1.01 | 4,056 B |
| SmartMapper | 100 | 745.7 ns | 26.04 ns | 75.96 ns | 0.93 | 4,056 B |

### Collection mapping — wrapper level (both return `CollectionWrapper`)

Both Direct and SmartMapper create `CollectionWrapper { Items = List<T> }`.

| Method | ItemCount | Mean | Error | StdDev | Ratio | Allocated |
|--------|----------:|-----:|------:|-------:|------:|----------:|
| Direct | 10 | 114.6 ns | 2.38 ns | 7.02 ns | 1.00 | 512 B |
| SmartMapper | 10 | 112.2 ns | 3.18 ns | 9.39 ns | 0.98 | 512 B |
| Direct | 100 | 891.4 ns | 25.95 ns | 76.51 ns | 1.01 | 4,112 B |
| SmartMapper | 100 | 916.5 ns | 27.90 ns | 82.27 ns | 1.04 | 4,112 B |

> **JIT analysis:**
> - **Simple / Conversion**: Disassembly confirms JIT generates identical or equivalent instructions. SmartMapper's conversion is faster because the specialized `ConvertToString(InvariantCulture)` path avoids boxing.
> - **Nested (1.24x)**: Disassembly shows both Direct and SmartMapper compile to equivalent code (155 vs 157 bytes) after full inlining of `MapNested` + `MapAddress`. The reported ratio has high variance (StdDev 1.77 ns vs 0.58 ns for Direct, P90 = 15.84 ns vs 11.75 ns), pointing to loop-back branch prediction noise rather than a code quality difference.
> - **Void nested**: The lambda-free multi-statement pattern (LegacyLambda 1.03x → SmartMapper 1.01x) confirms elimination of the closure allocation overhead.
> - **Collection**: Both scenarios (item-level and wrapper-level) show SmartMapper within statistical noise of Direct (ratio 0.93–1.07). Allocation is identical in each scenario. The element mapper (`MapItem`) is fully inlined by JIT.

---

## Running Tests

### Unit Tests (`Smart.Mapper.Tests`)

Uses xUnit v3 with Microsoft Testing Platform.

```powershell
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj
```

You can also run tests from Visual Studio Test Explorer.

> **Note:** `dotnet test` is not supported on .NET 10 SDK due to a Microsoft Testing Platform / VSTest incompatibility. Use `dotnet run --project` instead.

### Source Generator Tests (`Smart.Mapper.Generator.Tests`)

Verifies that the Roslyn source generator produces correct output and emits the correct diagnostics.

```powershell
dotnet run --project Smart.Mapper.Generator.Tests/Smart.Mapper.Generator.Tests.csproj
```

### NativeAOT Smoke Tests (`Smart.Mapper.AotTests`)

Verifies that the generated mapper code works correctly under NativeAOT publish.

**1. Publish as NativeAOT**

```powershell
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64
```

> Supported RIDs: `win-x64`, `linux-x64`, etc. Adjust to match your platform.

**2. Run the published executable**

```powershell
.\Smart.Mapper.AotTests\bin\Release\net10.0\win-x64\publish\Smart.Mapper.AotTests.exe
```

**3. Verify the output**

All 8 scenarios must pass:

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

If any test fails, the process exits with a non-zero exit code and prints `FAIL: <message>` to standard error.

**4. Check for AOT warnings (optional)**

```powershell
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64 2>&1 |
    Select-String "IL2|IL3"
```

No `IL2xxx` / `IL3xxx` diagnostics should appear.

---

## TODO

Future improvements under consideration:

- **`[MapCollection]` / `[MapNested]` targeting init-only or required members** — currently rejected with `SMP0212` because the generated loop runs after construction. Could be supported by hoisting the built collection / nested instance into a local before construction and assigning it in the object initializer.
- **Direct `FrozenSet` construction** — the generated code builds a `HashSet<T>` and calls `ToFrozenSet` (two-phase by BCL design). If the BCL ever ships a frozen-collection builder API, the intermediate set can be eliminated.
- **Generic fallback `Convert<TSource, TDestination>` for `Half` / `Int128` / `UInt128` / `BigInteger` sources** — these currently reach the boxing fallback when routed through the generic converter opt-in; specialized branches can be added if demand arises (the default specialized-method path already covers them).
- **Generator incrementality tuning** — output is regenerated per run via `Collect()` and destination/source property walks are repeated per feature pass. Measured cost is negligible today; revisit (per-class output splitting, property-list caching) if very large models appear.

---

## License

MIT
