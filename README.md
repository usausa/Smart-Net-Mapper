# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** is a high-performance object mapper library based on Roslyn Incremental Source Generator.
It automatically generates property-copying code at compile time for `partial` methods, static or instance, decorated with the `[Mapper]` attribute.

## Features

- **Zero overhead** - no reflection at runtime; all code is generated statically at compile time
- **Specialized-method dispatch** - `ConvertTo{TargetType}` naming convention enables direct-call generation, friendly to JIT inlining
- **Per-method declaration** - `[Mapper]` is placed on individual methods, static or instance, so mapper methods feel like ordinary helper functions, and an instance mapper can use the services injected into its class
- **Custom parameter passthrough** - additional arguments such as `Map(Src, Dst, TContext ctx)` go to the methods the attributes name and to the nested and element mappers that declare them, in any order, by type (by name for several of a type), and can be used in `[MapExpression]`
- **Culture-aware conversions** - the invariant culture by default, or the current culture, a culture name or a `CultureInfo` parameter, with date and number formats
- **NativeAOT / trimming fully supported** - `<IsAotCompatible>true</IsAotCompatible>` declared; NativeAOT smoke test passes
- **Rich diagnostics** - compile-time diagnostics in phase-based bands (SMP0001–SMP0503), with the cause and the fix of each in [Diagnostics.md](Diagnostics.md)

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

## Documentation

- [API reference](docs/API.md) - every attribute and rule in detail: auto-mapping, property paths, collections, constructors, null handling, type conversion, culture, strict mode and the edge cases
- [Diagnostics](Diagnostics.md) - the cause of each compile-time diagnostic and how to fix it
- [日本語](README.ja.md)

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

Generated code:

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

Same-name, compatible-type properties are mapped automatically, and the attributes below customize the mapping. A mapper can also be an extension method (`ToDestination(this Source source)`), be declared in a nested type, or be generic; see [Mapper methods](docs/API.md#mapper-methods).

## Attributes

| Attribute | Description |
|-----------|-------------|
| `[Mapper]` | Marks a mapper method; `AutoMap`, `Strict`, `NameComparison`, `Culture`, `DateTimeFormat`, `NumberFormat` |
| `[MapperProfile]` | Defaults for the mapper methods of a class, or of the assembly |
| `[MapProperty]` / `[MapProperty<T>]` | Maps a target from a source member or a dotted path; `Converter`, `NullValue`, `NullBehavior`, `Culture`, `DateTimeFormat`, `NumberFormat` |
| `[MapIgnore]` | Leaves a destination member out of the automatic mapping |
| `[MapUsing]` | Assigns the value a method computes from the source |
| `[MapFrom]` | Assigns the result of a parameterless source method, or a property path |
| `[MapConstant]` / `[MapConstant<T>]` | Assigns a constant |
| `[MapExpression]` | Assigns a C# expression (e.g. `"System.DateTime.Now"`) |
| `[MapCondition]` | Maps a target only when a condition method returns `true` |
| `[BeforeMap]` / `[AfterMap]` | Calls a method before / after the mapping |
| `[MapNested]` | Maps a member with a mapper method |
| `[MapCollection]` | Maps a collection element by element with a mapper method; `Strategy`, `Converter` |
| `[ValueConverter]` | Custom value converter class (method or class level) |
| `[CollectionConverter]` | Custom collection converter class (method or class level) |

For the attributes that map a destination member, the **first** argument is the **destination** (target) name. See the [attribute reference](docs/API.md#attribute-reference) for every property.

## Basic Usage

### Instance mappers

A mapper can be an instance method, and its attributes can then name instance methods of its class, which use its fields:

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

A static mapper naming an instance method is reported (SMP0105). See [Static and instance mappers](docs/API.md#static-and-instance-mappers).

### Renaming and nested properties

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]   // rename
[MapProperty(nameof(Destination.City), "Address.City")]           // flatten a nested source member
[MapProperty("Customer.Id", nameof(Source.CustomerId))]            // unflatten into a nested target member
public static partial void Map(Source source, Destination destination);
```

A nullable intermediate source member is checked for null, and an intermediate target member is created when it is null. Names are compared with `NameComparison` (`Ordinal` by default). See [Property paths](docs/API.md#property-paths) and [Name comparison](docs/API.md#name-comparison).

### Ignoring properties

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
public static partial void Map(Source source, Destination destination);

[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]   // only Id is mapped
public static partial void MapId(Source source, Destination destination);
```

See [`[MapIgnore]`](docs/API.md#mapignore).

### Computed and constant values

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]      // a method computing the value
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // a parameterless source method
[MapConstant(nameof(Destination.Status), "Active")]                    // a constant
[MapExpression(nameof(Destination.CreatedAt), "System.DateTime.Now")]  // a C# expression
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

See [`[MapUsing]`](docs/API.md#mapusing), [`[MapFrom]`](docs/API.md#mapfrom), [`[MapConstant]`](docs/API.md#mapconstant--mapconstantt), [`[MapExpression]`](docs/API.md#mapexpression) and [Assignment order](docs/API.md#assignment-order).

### Null handling

```csharp
// Source: string? Name, string? Note / Destination: string Name, string Note
[Mapper]
[MapProperty(nameof(Destination.Name), NullValue = "Unknown")]              // "Unknown" for a null source
[MapProperty(nameof(Destination.Note), NullBehavior = NullBehavior.Skip)]  // keeps the value for a null source
public static partial void Map(Source source, Destination destination);
```

Otherwise a null source gives the target `default`. See [Null handling](docs/API.md#null-handling).

### Conditions and callbacks

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

See [Callbacks and conditions](docs/API.md#callbacks-and-conditions).

### Nested objects and collections

```csharp
[Mapper]
public static partial ChildDto MapChild(Child source);

[Mapper]
[MapNested(nameof(ParentDto.Child), Mapper = nameof(MapChild))]
[MapCollection(nameof(ParentDto.Children), Mapper = nameof(MapChild))]
public static partial ParentDto Map(Parent source);
```

The collection loop is generated inline for lists, arrays, sets, dictionaries and immutable collections, and `Strategy = CollectionStrategy.InPlace` refills the existing collection instead of assigning a new one. A mapper of a collection as a whole, such as `List<ItemDto> Map(List<Item> source)`, is reported (SMP0007): map the elements with a mapper of the element type. See [Nested objects](docs/API.md#nested-objects) and [Collections](docs/API.md#collections).

### Records and constructors

```csharp
public record OrderDto(int Id, string Name);

[Mapper]
public static partial OrderDto Map(Order source);   // new OrderDto(source.Id, source.Name)
```

A return-type mapper chooses the constructor by the values it has, and sets `init`-only and `required` members in the object initializer. See [Constructors and records](docs/API.md#constructors-and-records).

### Type conversion

Conversions between the built-in types and `string`, between numbers, between enums (by member name), and through conversion operators or `Parse` are generated:

```csharp
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);   // string -> int
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);  // int -> string
```

A `[ValueConverter]` class replaces the converter of a method or a class, and the `Converter` of `[MapProperty]` converts a single property:

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

See [Type conversion](docs/API.md#type-conversion).

### Culture and formats

Conversions to and from text use the invariant culture by default (numbers with `InvariantCulture`, dates and times in the round-trip format `O`). A culture name, the current culture or a `CultureInfo` parameter changes that, and `DateTimeFormat` / `NumberFormat` give the formats:

```csharp
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]   // the current culture when no name applies

[MapperProfile(Culture = "ja-JP")]
internal static partial class AppMappers
{
    [Mapper(NumberFormat = "N2")]
    public static partial Dest Map(Src src);

    [Mapper]
    [MapProperty(nameof(Dest2.Amount), nameof(Src2.Price), Culture = "en-US", NumberFormat = "C")]
    public static partial Dest2 Map(Src2 src);

    [Mapper]
    public static partial Dest3 Map(Src3 src, CultureInfo culture);   // the caller gives the culture
}
```

The culture comes from `[MapProperty]`, then the `CultureInfo` parameter, `[Mapper]`, the class profile, the assembly profile, and last `DefaultCulture`. See [Culture and formats](docs/API.md#culture-and-formats).

### Profiles

`[MapperProfile]` on a class, or on the assembly, gives defaults to the mapper methods. `[Mapper]` wins over the class profile, which wins over the assembly profile, each setting on its own:

```csharp
[assembly: MapperProfile(NameComparison = StringComparison.OrdinalIgnoreCase)]

[MapperProfile(Strict = true)]
internal static partial class OrderMappers
{
    [Mapper]
    public static partial OrderDto Map(Order source);
}
```

See [Profiles](docs/API.md#profiles).

### Strict mode

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

Strict mode warns of destination members no mapping assigns (SMP0501), values that may be null going to targets that do not take null (SMP0502), and enum members without a member of the same name in the target enum (SMP0503). See [Strict mode](docs/API.md#strict-mode).

### Custom parameters

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

A method takes the custom parameters it declares, in any order, by type, or by name when several have the same type. The mappers of `[MapNested]` / `[MapCollection]` take them as well, so a `CultureInfo` parameter reaches the nested objects. See [Custom parameters](docs/API.md#custom-parameters).

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

```powershell
# Unit tests (Smart.Mapper.Tests, xUnit v3 with Microsoft Testing Platform)
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj

# With code coverage (Cobertura XML under TestResults in the output directory)
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj -- --coverage --coverage-settings CodeCoverage.runsettings

# Source generator tests (Smart.Mapper.Generator.Tests): the generated output and the diagnostics
dotnet run --project Smart.Mapper.Generator.Tests/Smart.Mapper.Generator.Tests.csproj

# NativeAOT smoke tests (Smart.Mapper.AotTests): publish with the RID of your platform (win-x64, linux-x64, ...) and run
dotnet publish Smart.Mapper.AotTests/Smart.Mapper.AotTests.csproj -c Release -r win-x64
.\Smart.Mapper.AotTests\bin\Release\net10.0\win-x64\publish\Smart.Mapper.AotTests.exe
```

`dotnet test` is not supported on .NET 10 SDK due to a Microsoft Testing Platform / VSTest incompatibility; use `dotnet run --project`, or Visual Studio Test Explorer. The AOT smoke test prints `[OK]` for each of its 8 scenarios (basic void mapping, basic return mapping, type conversion, enum mapping, null handling, nested property mapping, collection mapping and a custom value converter) and ends with `All AOT smoke tests passed.`; a failure prints `FAIL: <message>` to standard error and exits with a non-zero code. The publish output should show no `IL2xxx` / `IL3xxx` warnings (`dotnet publish ... 2>&1 | Select-String "IL2|IL3"`).

---

## TODO

- **Direct `FrozenSet` construction** — the generated code builds a `HashSet<T>` and calls `ToFrozenSet` (two-phase by BCL design); a frozen-collection builder API in the BCL would eliminate the intermediate set.
- **Generic fallback `Convert<TSource, TDestination>` for `Half` / `Int128` / `UInt128` / `BigInteger` sources** — these reach the boxing fallback when routed through the generic converter opt-in; specialized branches can be added if demand arises (the default specialized-method path already covers them).
- **Generator incrementality** — models are cached per mapper method and sources per class, so an edit regenerates only the classes whose mappers it changes, with the property lists, type lookups and converter lookups shared within a compilation; the grouping of the models by class still goes over all of them on each edit (`Collect()`), which is cheap today and can be split further if very large projects need it.

---

## License

MIT
