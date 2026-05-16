# Smart-Net-Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper)

## Overview

Smart.Mapper is a high-performance object mapper for .NET built on a Roslyn Incremental Source Generator.
Mapping code is generated at compile time from `[Mapper]`-annotated `partial` methods, so there is zero reflection, zero runtime configuration, and the JIT can fully inline the generated assignments.

- **Target frameworks:** `net10.0` / `net9.0` / `net8.0`
- **Package:** [`Usa.Smart.Mapper`](https://www.nuget.org/packages/Usa.Smart.Mapper) (analyzer is bundled — no separate generator package needed)
- **Declaration style:** per-method `[Mapper]` attribute on a `partial` method inside a `static partial class`

## Installation

```
dotnet add package Usa.Smart.Mapper
```

## Quick Start

```csharp
using Smart.Mapper;

internal static partial class ObjectMapper
{
    // Auto-mapping: same-name properties are mapped automatically.
    [Mapper]
    public static partial void Map(Source source, Destination destination);

    // Return-style mapping: destination is constructed and returned.
    [Mapper]
    public static partial Destination MapToNew(Source source);

    // Rename and ignore.
    [Mapper]
    [MapProperty(nameof(Destination.DisplayName), nameof(Source.Name))]
    [MapIgnore(nameof(Destination.Secret))]
    public static partial void MapRenamed(Source source, Destination destination);
}
```

The Source Generator emits the implementation directly into the partial class — no runtime configuration, no IoC registration.

## Features

### Mapping basics

- Same-name automatic mapping (`[Mapper]`, opt out via `AutoMap = false`)
- Property renaming (`[MapProperty("Target", "Source")]`)
- Ignore (`[MapIgnore]`)
- Strict mode (`[Mapper(Strict = true)]`) — emits **ML0017** for unmapped destination properties
- Name comparison strategy (`[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]`)
- Order control (`Order` property on every mapping attribute)

### Values and computation

- Constant values (`[MapConstant]`, `[MapConstant<T>]` with C# 11 generic attributes)
- Inline expressions (`[MapExpression("System.DateTime.Now")]`)
- Computed values via static method (`[MapUsing("Target", nameof(Compute))]`)
- Source method / property-path projection (`[MapFrom("Target", "Nested.Value")]` / `[MapFrom("Target", nameof(Source.GetSomething))]`)

### Type conversion

- Built-in conversions for primitives, `string`, `Guid`, `DateTime`, `Nullable<T>` (`DefaultValueConverter`)
- C#-spec compliant numeric widening
- Custom converter via type (`[MapConverter(typeof(MyConverter))]`)
- Per-property converter method (`[MapProperty(..., Converter = nameof(MyConvert))]`)
- **Specialized method dispatch** — if your converter defines `ConvertTo{TargetType}(...)`, the generator emits a direct call to that method instead of going through a generic `Convert<TS, TD>` shim, improving inlining and avoiding boxing.

### Nesting and collections

- Dot-notation flatten / unflatten (`[MapProperty("A.B.C", "X")]` / `[MapProperty("X", "A.B.C")]`)
- Nested object mapping with child mapper (`[MapNested("Child", "Child", Mapper = nameof(MapChild))]`)
- Collection mapping (`[MapCollection("Items", "Items", Mapper = nameof(MapItem))]`)
- Custom collection converter (`[CollectionConverter(typeof(MyConverter))]`)

### Null handling

- `NullBehavior.Default` (default) — null source falls back to a sensible default value
- `NullBehavior.Skip` — leave the destination property untouched on null source
- `NullSubstitute = ...` (also typed `[MapProperty<T>(... NullSubstitute = ...)]`) — explicit substitute value

### Hooks and context

- `[BeforeMap(nameof(OnBeforeMap))]` / `[AfterMap(nameof(OnAfterMap))]`
- Custom parameter (context) — any extra parameters on the `[Mapper]` method are propagated to `BeforeMap` / `AfterMap` / `Converter` / `Condition` / `MapUsing` / `MapFrom`

### Other

- `struct` source / destination supported
- Nullable reference type aware (works with `<Nullable>enable</Nullable>`)
- 17 compile-time diagnostics (`ML0001`–`ML0017`) catch bad signatures, unknown members, duplicate targets, and more

## Attribute Reference

| Attribute | Purpose |
|---|---|
| `[Mapper]` | Marks a `partial` method as a mapper. Options: `AutoMap`, `Strict`, `NameComparison`. |
| `[MapProperty]` / `[MapProperty<T>]` | Rename / convert / null-handle a property. Supports `Converter`, `NullBehavior`, `NullSubstitute`, `Order`, dot-notation paths. |
| `[MapIgnore]` | Exclude a destination property from auto-mapping. |
| `[MapConstant]` / `[MapConstant<T>]` | Assign a constant value. |
| `[MapExpression]` | Assign an arbitrary inline expression (string). |
| `[MapUsing]` | Compute a value via a static method. |
| `[MapFrom]` | Pull a value from a source method or a property path. |
| `[MapCondition]` | Predicate-controlled per-property mapping. |
| `[MapNested]` | Map a nested object through a child mapper. |
| `[MapCollection]` | Map a collection through an element mapper. |
| `[MapConverter]` | Register a type-level custom converter (class / struct / method). |
| `[CollectionConverter]` | Register a collection-level custom converter. |
| `[BeforeMap]` / `[AfterMap]` | Pre / post hooks for the mapping. |

## Diagnostics

All diagnostics are emitted under the `Smart.Mapper` category.

| ID | Severity | Summary |
|---|---|---|
| ML0001 | Error | Mapper method must be `static partial`. |
| ML0002 | Error | Mapper method needs a valid parameter signature. |
| ML0003 | Error | Custom parameter types must be unique. |
| ML0004 / ML0005 | Error | Invalid `BeforeMap` / `AfterMap` signature. |
| ML0006 | Error | Invalid `Converter` signature. |
| ML0007 / ML0008 | Error | Invalid `Condition` / property-level condition signature. |
| ML0009 / ML0010 | Error | Invalid `MapFrom` / `MapFromMethod` signature. |
| ML0011 / ML0012 | Error | `MapFrom` return type mismatch. |
| ML0013 | Error | Invalid `MapCollection` mapper method. |
| ML0014 | Error | Invalid `MapNested` mapper method. |
| ML0015 | Error | Duplicate target mapping. |
| ML0016 | Warning | Mapping attribute on a `[MapIgnore]`-marked property. |
| ML0017 | Warning | Unmapped destination property (strict mode). |

## Documentation

- [`docs/SPECIFICATION.md`](docs/SPECIFICATION.md) — full feature specification (Japanese)
- [`docs/MAPPER_GENERATOR_ARCHITECTURE.md`](docs/MAPPER_GENERATOR_ARCHITECTURE.md) — generator internals (Japanese)
- [`docs/REVIEW.md`](docs/REVIEW.md) — implementation review (Japanese)
- [`docs/PROPOSALS.md`](docs/PROPOSALS.md) — roadmap and feature proposals (Japanese)
- [`docs/COMPARISON.md`](docs/COMPARISON.md) — comparison with Mapperly / Mapster / NextGenMapper / AutoMapper (Japanese)

## Benchmark

A historical run is provided below for reference; numbers were captured on .NET 6 / Windows 10 with an older revision of the library and predate the current target frameworks and the specialized-method-dispatch implementation. The benchmark project (`Smart.Mapper.Benchmark`, BenchmarkDotNet) is included in the repository and a fresh run on .NET 8 / 9 / 10 is planned ahead of the 1.0 release.

``` ini
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.22000
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.100
  [Host]    : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  MediumRun : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2
WarmupCount=10
```

|                      Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |       P90 |  Gen 0 | Allocated |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|-------:|----------:|
|            SimpleAutoMapper | 65.466 ns | 0.9948 ns | 1.4267 ns | 64.705 ns | 63.738 ns | 67.147 ns | 67.078 ns | 0.0038 |      64 B |
|           SimpleAutoMapper2 | 64.700 ns | 0.2078 ns | 0.2981 ns | 64.724 ns | 63.975 ns | 65.383 ns | 65.067 ns | 0.0038 |      64 B |
|            SimpleTinyMapper | 24.601 ns | 0.2740 ns | 0.4101 ns | 24.602 ns | 23.903 ns | 25.191 ns | 25.099 ns | 0.0038 |      64 B |
|         SimpleInstantMapper | 61.863 ns | 0.2972 ns | 0.4167 ns | 61.856 ns | 61.064 ns | 62.838 ns | 62.292 ns | 0.0095 |     160 B |
|             SimpleRawMapper | 33.114 ns | 0.3367 ns | 0.5040 ns | 32.896 ns | 32.317 ns | 34.165 ns | 33.866 ns | 0.0038 |      64 B |
|           SimpleSmartMapper | 11.746 ns | 0.0430 ns | 0.0644 ns | 11.759 ns | 11.607 ns | 11.880 ns | 11.810 ns | 0.0038 |      64 B |
| SimpleInstantMapperWoLookup | 54.184 ns | 0.1680 ns | 0.2515 ns | 54.159 ns | 53.744 ns | 54.609 ns | 54.549 ns | 0.0095 |     160 B |
|     SimpleRawMapperWoLookup | 24.994 ns | 0.0704 ns | 0.1054 ns | 24.990 ns | 24.810 ns | 25.174 ns | 25.137 ns | 0.0038 |      64 B |
|   SimpleSmartMapperWoLookup |  8.704 ns | 0.0290 ns | 0.0424 ns |  8.702 ns |  8.643 ns |  8.810 ns |  8.747 ns | 0.0038 |      64 B |
|                SimpleDirect |  8.028 ns | 0.0249 ns | 0.0365 ns |  8.029 ns |  7.957 ns |  8.102 ns |  8.078 ns | 0.0038 |      64 B |
|                SimpleInline |  7.407 ns | 0.0346 ns | 0.0518 ns |  7.395 ns |  7.341 ns |  7.553 ns |  7.476 ns | 0.0038 |      64 B |
|             MixedAutoMapper | 62.985 ns | 0.7563 ns | 1.1319 ns | 62.989 ns | 61.427 ns | 66.342 ns | 63.971 ns | 0.0038 |      64 B |
|            MixedAutoMapper2 | 61.519 ns | 0.2114 ns | 0.2964 ns | 61.561 ns | 60.778 ns | 62.014 ns | 61.834 ns | 0.0038 |      64 B |
|             MixedTinyMapper | 39.092 ns | 0.3748 ns | 0.5610 ns | 39.103 ns | 38.306 ns | 39.904 ns | 39.798 ns | 0.0067 |     112 B |
|          MixedInstantMapper | 78.591 ns | 0.2413 ns | 0.3612 ns | 78.470 ns | 77.880 ns | 79.463 ns | 79.054 ns | 0.0123 |     208 B |
|              MixedRawMapper | 32.251 ns | 0.3167 ns | 0.4642 ns | 32.313 ns | 31.508 ns | 33.211 ns | 32.889 ns | 0.0038 |      64 B |
|            MixedSmartMapper |  8.576 ns | 0.0256 ns | 0.0376 ns |  8.575 ns |  8.507 ns |  8.657 ns |  8.619 ns | 0.0038 |      64 B |
|            SingleAutoMapper | 58.018 ns | 0.3764 ns | 0.5398 ns | 58.054 ns | 57.280 ns | 58.918 ns | 58.611 ns | 0.0014 |      24 B |
|           SingleAutoMapper2 | 58.242 ns | 0.5740 ns | 0.8233 ns | 57.793 ns | 57.103 ns | 59.245 ns | 59.166 ns | 0.0014 |      24 B |
|            SingleTinyMapper | 20.350 ns | 0.0811 ns | 0.1213 ns | 20.323 ns | 20.158 ns | 20.616 ns | 20.544 ns | 0.0014 |      24 B |
|         SingleInstantMapper | 18.291 ns | 0.1142 ns | 0.1674 ns | 18.290 ns | 18.042 ns | 18.586 ns | 18.503 ns | 0.0029 |      48 B |
|             SingleRawMapper | 14.929 ns | 0.1577 ns | 0.2311 ns | 15.074 ns | 14.547 ns | 15.209 ns | 15.181 ns | 0.0014 |      24 B |
|           SingleSmartMapper |  6.144 ns | 0.0197 ns | 0.0289 ns |  6.144 ns |  6.095 ns |  6.201 ns |  6.176 ns | 0.0014 |      24 B |

## License

MIT
