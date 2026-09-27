# Smart.Mapper

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)
[![NuGet](https://img.shields.io/nuget/dt/Usa.Smart.Mapper.svg)](https://www.nuget.org/packages/Usa.Smart.Mapper/)

**Smart.Mapper** is a high-performance object mapper library based on Roslyn Incremental Source Generator.
It automatically generates property-copying code at compile time for `static partial` methods decorated with the `[Mapper]` attribute.

## Features

- **Zero overhead** - no reflection at runtime; all code is generated statically at compile time
- **Specialized-method dispatch** - `ConvertTo{TargetType}` naming convention enables direct-call generation, friendly to JIT inlining
- **Per-method declaration** - `[Mapper]` is placed on individual methods, so mapper methods feel like ordinary helper functions
- **Custom parameter passthrough** - additional arguments such as `Map(Src, Dst, TContext ctx)` are passed on to the `[MapUsing]`, `Converter`, `[MapCondition]`, `[BeforeMap]` and `[AfterMap]` methods that declare them, and can be used in `[MapExpression]`
- **NativeAOT / trimming fully supported** - `<IsAotCompatible>true</IsAotCompatible>` declared; NativeAOT smoke test passes
- **Rich diagnostics** - 45 compile-time diagnostics in phase-based bands (SMP0001–SMP0501)

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

A struct destination is taken by `ref`, as in `Map(Source source, ref Destination destination)`: passed by value, it would be a copy the caller never sees filled, and passed as `in` or `ref readonly`, its members could not be assigned (SMP0005).

### Generated code (return pattern)

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

### Extension method mappers

A `[Mapper]` method can be declared as an extension method. The generated implementation keeps the `this` modifier, so the mapper reads naturally at the call site.

```csharp
public static partial class ObjectMapper
{
    [Mapper]
    public static partial Destination ToDestination(this Source source);
}

var destination = source.ToDestination();
```

### Mappers in nested types

A `[Mapper]` method may be declared in a nested type. The generated code declares the containing types again, outermost first, with their kinds (`class`, `struct`, `record`, `record struct`) and type parameters, so each of them has to be `partial` (SMP0001 otherwise).

```csharp
public static partial class Mappers
{
    public static partial class Orders
    {
        [Mapper]
        public static partial OrderDto Map(Order source);
    }
}

// Generated:
partial class Mappers
{
    partial class Orders
    {
        public static partial OrderDto Map(Order source) { ... }
    }
}
```

### Generic mapper methods

A `[Mapper]` method may be generic. The generated implementation repeats its type parameters and their constraints, and a type parameter used as the source or the destination has the properties of its constraint types.

```csharp
public static partial class Mappers
{
    [Mapper]
    public static partial PageDto<T> Map<T>(Page<T> source);

    [Mapper]
    public static partial T Create<T>(EntitySource source) where T : Entity, new();
}
```

A destination type parameter is created with `new T()`, so it needs the `new()` or the `struct` constraint (SMP0305 otherwise).

---

## Attribute Reference

### Method-level attributes

| Attribute | Description |
|-----------|-------------|
| `[Mapper]` | Marks a method as a mapping method |
| `[Mapper(AutoMap = false)]` | Disables automatic same-name property mapping |
| `[Mapper(Strict = true)]` | Emits SMP0501 warning for unmapped destination properties |
| `[Mapper(NameComparison = ...)]` | Property name comparison mode, applied to auto-mapping and to the names written in mapping attributes (default: `Ordinal`) |
| `[Mapper(Culture = "...")]` | Culture used for type conversion (e.g., `"ja-JP"`) |
| `[Mapper(DateTimeFormat = "...")]` | Format string for `DateTime` <-> `string` conversion (use with `Culture`) |
| `[Mapper(NumberFormat = "...")]` | Format string for numeric <-> `string` conversion (use with `Culture`) |
| `[MapProperty]` | Explicit property-to-property mapping; supports `NullValue`, `NullBehavior`, `Culture`, `DateTimeFormat`, `NumberFormat`, `Converter` |
| `[MapProperty<T>]` | Type-safe variant of `[MapProperty]` (C# 11+) |
| `[MapUsing]` | Calculates a value via a static method (custom-parameter aware) |
| `[MapFrom]` | Maps from a source instance-method call or dot-notation property path |
| `[MapConstant]` | Sets a constant value on a destination property |
| `[MapConstant<T>]` | Type-safe variant of `[MapConstant]` (C# 11+) |
| `[MapExpression]` | Embeds an arbitrary C# expression (e.g., `"System.DateTime.Now"`) |
| `[MapIgnore]` | Excludes a destination property from mapping |
| `[BeforeMap]` | Callback invoked before mapping |
| `[AfterMap]` | Callback invoked after mapping |
| `[MapCondition]` | Maps a destination property only when a condition method returns `true` |
| `[MapCollection]` | Collection property mapping via an explicit mapper method; supports `Strategy`, `Converter` |
| `[MapNested]` | Nested object mapping via an explicit mapper method |
| `[ValueConverter]` | Custom type converter (method / class level); supports `Method` |
| `[CollectionConverter]` | Custom collection converter (method / class level) |

> **First argument convention** - For the attributes that map a destination member, the **first** argument is the **destination** (target) name. The **second** is the source for `[MapProperty]`, `[MapFrom]`, `[MapCollection]` and `[MapNested]`, and the method, constant or expression for `[MapUsing]`, `[MapCondition]`, `[MapConstant]` and `[MapExpression]`.

> **Keywords as names** - A name that is a C# keyword, such as `@class`, keeps its `@` in the generated code, so members, enum members, parameters, methods, classes and namespaces named that way map like any other.

### Class-level attributes

| Attribute | Description |
|-----------|-------------|
| `[MapperProfile]` | Sets defaults (`Strict`, `NameComparison`, `Culture`, `DateTimeFormat`, `NumberFormat`) for all `[Mapper]` methods in the class; method-level settings take precedence, each on its own |
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

A property the mapper cannot assign (get-only, or a setter it cannot call such as `private set`) is left out, unless the constructor that construction calls takes it. Named in a mapping attribute, such a property is reported (SMP0214).

The properties a type inherits are included: those of its base classes, and for an interface, those of the interfaces it extends. A name is taken as `x.Name` binds to it in the generated code: to the most derived member of the name the mapper class can access, whatever it is. A property overriding one of a base type, or hiding it with `new`, stands for it, and one overriding the getter only is assigned through the setter it inherits. A name whose member is not a public instance property (an `internal` property, a field, a method) is not mapped automatically, nor is the property it hides; a `private` member hides nothing from the mapper class. A name an interface inherits from two interfaces, neither hiding the other, is ambiguous and not mapped either. Indexers are left out. The same holds for the names written in the mapping attributes. A source property is read through its getter, so one without a getter the mapper class can call (a `private get`, or set-only) is not a source.

### Property remapping (`[MapProperty]`)

```csharp
[Mapper]
[MapProperty(nameof(Destination.FullName), nameof(Source.Name))]
public static partial void Map(Source source, Destination destination);
```

The source name may be omitted, in which case it defaults to the target name. This is handy when only an option needs to be set on an otherwise same-named pair:

```csharp
[Mapper]
[MapProperty(nameof(Destination.Amount), Culture = "en-US", NumberFormat = "C")]
public static partial void Map(Source source, Destination destination);
```

`[MapNested]` and `[MapCollection]` follow the same rule.

A name that cannot be resolved is reported (`SMP0213` for the source, `SMP0214` for the target) rather than silently dropped. So is the target of `[MapIgnore]` and `[MapCondition]` that does not exist: a property or field of the destination, a dotted path of them (for `[MapCondition]`), or a parameter of the constructor a return-type mapper calls.

A member mapped as a whole by one attribute and a member of it by a dotted path of another (`Child` and `Child.Value`) cannot both apply, and are reported as mapping the same target (SMP0101).

### Name comparison (`NameComparison`)

`NameComparison` applies to the names written in mapping attributes, not just to auto-mapping. The comparison is the one of `[Mapper]`, or else the one of `[MapperProfile]`, or else `Ordinal`. An exact match always wins; the configured comparison is only a fallback, so the default (`Ordinal`) behaves as before.

```csharp
public class Src { public int other { get; set; } }
public class Dst { public int Value { get; set; } }

[Mapper(NameComparison = StringComparison.OrdinalIgnoreCase)]
[MapProperty("value", "Other")]   // neither spelling matches the declaration exactly
public static partial Dst Map(Src src);
```

Generated code — both names resolve to the declared members:

```csharp
__d.Value = src.other;
```

This holds for every name of a member written in a mapping attribute: the targets, properties and fields alike and each segment of a dotted path, including target-only attributes such as `[MapIgnore]`, the sources, and the member of `[MapFrom]`. When several members match ignoring case, the first one declared wins, a property before a field. The names of methods (`Converter`, `[MapCondition]`, `[MapUsing]`, `[BeforeMap]` / `[AfterMap]`, and the mappers of `[MapCollection]` / `[MapNested]`) are C# identifiers and are matched exactly.

### Null substitution (`NullValue`)

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullValue = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullValue = 0)]
public static partial void Map(Source source, Destination destination);
```

The value is written like a `[MapConstant]` value and has to convert to the target type, as in `source.Count ?? 0` (SMP0218 otherwise); `NullValue = null` needs a target that takes null.

A reference declared with nullable annotations disabled may hold null as well, so `NullValue` and `NullBehavior.Skip` apply to it as they do to one declared nullable.

With a `Converter`, which takes the source as it is, a null source takes `NullValue`, and the converter is called for a value only, as in `source.Count is not null ? ToText(source.Count) : "none"`. A dotted source whose intermediate member is null takes `NullValue` as well (see Nested Property Mapping).

A converter whose parameter does not take null (a reference annotated as not null, or `[DisallowNull]`) is not given a null source either: it is called for a value only. A null source takes `NullValue`; without one, it leaves the target as it is in an assignment, and gives `null` to a target that takes it, or `default`, as a constructor argument or an object initializer entry. A converter whose parameter takes null (`string?`, `[AllowNull]`, or one declared with nullable annotations disabled) gets the source as it is. A converter taking the struct a nullable struct source holds (an `int` for an `int?`), or a type it converts to implicitly (a `long` or a `double`), gets its `Value`, for a value only in the same way, as in `source.Count is not null ? ToText(source.Count.Value) : "none"`.

```csharp
// Source: string? Name / Destination: string Name
// [MapProperty(nameof(Destination.Name), Converter = nameof(Trim))], private static string Trim(string value)
if (source.Name is not null)
{
    destination.Name = Trim(source.Name);
}
```

### Keeping the destination value (`NullBehavior.Skip`)

With `NullBehavior.Skip`, a null source leaves the destination member as it is instead of assigning a value:

```csharp
// Source: string? Name, int? Count / Destination: string Name, string Count
[Mapper]
[MapProperty(nameof(Destination.Name), NullBehavior = NullBehavior.Skip)]
[MapProperty(nameof(Destination.Count), NullBehavior = NullBehavior.Skip)]
public static partial void Map(Source source, Destination destination);
```

Generated code:

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

A `Converter`, which takes the source as it is, is then called only for a value as well.

A member assigned through a constructor or an object initializer has no previous value to keep, so `NullBehavior.Skip` is rejected there (SMP0215).

### Ignore properties (`[MapIgnore]`)

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

The target is a member of the destination as a whole: a dotted target (`Child.Value`) is reported (SMP0223), as the automatic mapping never assigns a member of a member on its own, so there is nothing to leave out.

`[MapIgnore]` and an attribute mapping the same target contradict each other and are reported (SMP0101), whichever the attribute is. `[MapIgnore]` of a member with dotted paths into it is allowed: the member is left out of the automatic mapping, and the paths are applied.

A member a parameter of the constructor of a return-type mapper assigns, or a parameter no member has, can be ignored as well: the parameter has no value then, so another constructor is chosen, an optional parameter is left out, or the destination is created without arguments (see [Choosing the constructor](#choosing-the-constructor)). A destination that cannot be created otherwise is reported (SMP0216).

A `required` member of the destination a return-type mapper creates cannot be ignored, as it has to be set when the destination is constructed (SMP0216), unless the constructor called has `[SetsRequiredMembers]`.

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

The `Converter` of `[MapProperty]`, `[MapCondition]` and `[BeforeMap]` / `[AfterMap]` methods receive them the same way when they declare them after their usual parameters, and a `[MapExpression]` can refer to them by name. The mapper methods of `[MapCollection]` / `[MapNested]` and the methods of `[ValueConverter]` / `[CollectionConverter]` classes do not receive them.

The methods these attributes name (`[MapUsing]` methods, converters, conditions, callbacks and the mappers of `[MapCollection]` / `[MapNested]`) are static methods the generated code calls by their simple name, so they are looked up as C# looks the name of a call up: in the mapper class and its base classes (a `protected` method included, as the mapper class can call it), or, when none of them has a member of the name the call can invoke, in the class containing the mapper class and its base classes, and so on outward, and last in the types the `global using static` directives import, which the generated file sees as well (a `using static` directive of one file only is not seen, and a type imported gives the methods it declares, not those it inherits nor its extension methods). As in C#, the first class having a member of the name the call can invoke, which the mapper class can access, is the only one looked in, even when its methods do not take the arguments; a member the call cannot invoke, such as a property or a field that is not a delegate, or a nested type, is passed over. A method of a derived class hides one of a base class of the same signature. Instance methods are not taken. The methods are called without type arguments, so a generic one is not taken: it is reported as a method that does not match.

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
        [MapProperty(nameof(OrderDto.Total), Converter = nameof(FormatMoney))]  // of the base class
        [MapProperty(nameof(OrderDto.Name), Converter = nameof(Upper))]         // of the containing class
        public static partial OrderDto Map(Order source);
    }
}
```

Methods shared by several mapper classes can live in a class imported with `global using static` (`global using static MyApp.Converters;` in one file of the project), or in a base class the mapper classes derive from. A conversion applied by the types of the source and the target, rather than named for each property, goes to a `[ValueConverter]` class (see Custom value converter).

The method of `[MapUsing]`, the `Converter` of `[MapProperty]` and the method of `[MapCondition]` take the value (the source for `[MapUsing]`, the source member for the others) as its own type, or, to a parameter taken by value, as a type it converts to implicitly, as C# passes it: a base class or an interface (`private static string Label(IHasName x)` for a `Person` source, a converter taking `IEnumerable<string>` for a `List<string>` member), `object` or an interface a struct implements (boxing, `IFormattable` for a `DateTime`), a wider number (`long` for an `int`), a nullable struct (`int?` for an `int`), and a user-defined implicit conversion, not one obsolete as an error. A parameter taken by `in` takes the type of the value only. A nullable struct goes to a converter or a condition taking the struct it holds (an `int` for an `int?`), or, by value, a type the value it holds converts to implicitly (a `long` or a `double`, or a user-defined conversion), as its `Value`, for a value only, as to a parameter that does not take null (see Null substitution and Conditional mapping): a method taking the struct itself goes first, then one taking the nullable struct by a conversion (`long?`, `object`), then one taking the value it holds by a conversion. A value going to a parameter through a user-defined conversion goes for a value only as well when the parameter of the conversion operator does not take null. A method only an explicit conversion takes the value to, such as a base class to a derived class, does not match, and is reported (SMP0104, SMP0106, SMP0201).

Of the overloads, the call binds to the one C# binds it to: one taking the type of the value first, and otherwise the one whose parameter is the most specific (a class over its base class or interface, `long` over `object` for an `int`). The call has to bind to the method chosen: one taking the type of the value it does not bind to gives way to the one it binds to (a method of a derived class taking the value leaves out those of its base classes, and one taking by value goes before one taking by `in`). A call that is ambiguous (CS0121), or that binds, or may bind, to a method not matched (one taking the value by an explicit conversion, a generic one, one with optional parameters or `params`, or one obsolete as an error) is reported as a method that does not match, and one binding to a method that returns another type by its return type (SMP0105, SMP0202, and SMP0106 for a condition not returning `bool`).

The method of `[MapUsing]` returns the type of the target, or a type C# converts to it implicitly, as the assignment does: an `int` for a `long` or an `int?` target, a class for a base class or an interface it implements. One only an explicit conversion takes, such as a `long` for an `int` target, is reported (SMP0202). A nullable reference it returns into a target not annotated as nullable is taken with `!`, as `[MapFrom]` takes it.

### Source method / property-path (`[MapFrom]`)

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // instance method call
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // dot-notation path
public static partial void Map(Source source, Destination destination);
```

The method is a parameterless instance method the mapper class can call, not a generic one, found the way the call `source.Method()` binds: one of a base class, or of an interface the source interface extends, is found as well, the most derived one first, so that a method hiding another (`new`) wins. A property path goes through the inherited properties the same way.

The member gives the type of the target, or a type converting to it implicitly, as the method of `[MapUsing]` does (SMP0205 otherwise). A property path through members that may be null is read under their null check, as the source path of `[MapProperty]` is: the target is left as it is when one of them is null, and a constructor argument or an object initializer entry gets `null` for a target that takes it, or `default`. A nullable reference going to a target not annotated as nullable is taken with `!`, as `[MapProperty]` takes it.

```csharp
// [MapFrom(nameof(Destination.City), "Customer.Address.City")], Customer and Address nullable
if (source.Customer is not null && source.Customer.Address is not null)
{
    destination.City = source.Customer.Address.City;
}
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

The constant is written as an expression of its own type: an enum as its member (as a cast for a value no member has, such as a combination of flags), a type as `typeof`, an array as a new array for each mapping, and a number, a character or a string as a C# literal spelled the same under any culture (`double.NaN` and the infinities by name, quotes and control characters escaped). A `byte`, `sbyte`, `short` or `ushort`, which has no literal of its own, is written as a cast, so that it keeps its type where the target takes any value (an `object` boxes it as a `short`, not an `int`).

```csharp
[MapConstant(nameof(Destination.Kind), Kind.Active)]                  // __d.Kind = global::Sample.Kind.Active;
[MapConstant(nameof(Destination.Access), Access.Read | Access.Write)] // __d.Access = (global::Sample.Access)3;
[MapConstant(nameof(Destination.ItemType), typeof(Item))]             // __d.ItemType = typeof(global::Sample.Item);
[MapConstant(nameof(Destination.Codes), new[] { 1, 2 })]              // __d.Codes = new int[] { 1, 2 };
[MapConstant(nameof(Destination.Ratio), 0.1)]                         // __d.Ratio = 0.1d;
[MapConstant(nameof(Destination.Flag), (short)-1)]                    // __d.Flag = (short)-1;
[MapConstant(nameof(Destination.Note), "tab\there")]                  // __d.Note = "tab\there";
```

It has to convert to the target type the way the compiler converts it (`1` to a `long` or a `byte`, `null` to a reference): `"abc"` for an `int`, `1.5` for a `float`, a `short` for a `byte`, an enum for a number or another enum, and `null` (or an array holding `null`) for a reference annotated as not null are reported (SMP0218), as is a value the generated code cannot refer to, such as a file-local type (SMP0220).

The target of `[MapConstant]`, `[MapExpression]` and `[MapUsing]` is a property or field the mapper can assign, also through a dotted path such as `"Child.Value"`, which goes through its intermediate members as a `[MapProperty]` path does (see Nested Property Mapping); one that is not found (a misspelled name, a method or a static member) or cannot be assigned (a `readonly` field) is reported (SMP0214). The method of `[MapUsing]` is matched against the type of the target, a dotted one or a field as well.

An expression is compiled as a static local function that takes the mapper's parameters under the same names, so it can refer to them (e.g. `"source.Price * source.Quantity"`), and variables it declares with `out var` or patterns do not clash with those of other expressions.

### Before / After Map callbacks

```csharp
[Mapper]
[BeforeMap(nameof(BeforeMapping))]
[AfterMap(nameof(AfterMapping))]
public static partial void Map(Source source, Destination destination);

private static void BeforeMapping(Source source, Destination destination) { /* ... */ }
private static void AfterMapping(Source source, Destination destination) { /* ... */ }
```

A callback takes the source and the destination as their types, or, by value, as base classes or interfaces they convert to, so that one callback serves several mappers: `private static void Audit(IEntity source, IAuditable destination)`. A struct source or destination goes to its own type only, as a boxed copy would take what the callback writes. The custom parameters follow them, as their own types. Of the overloads, the one the call binds to is used, as C# binds it, and an ambiguous call is reported (SMP0102 / SMP0103).

### Conditional mapping (`[MapCondition]`)

The destination property is assigned only when the condition method, which takes the source value (and the custom parameters), returns `true`.

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

A condition guards the property mapping of its target: the automatic one, or a `[MapProperty]`, also through a dotted path. On a target no property mapping assigns, one nothing maps, one `[MapIgnore]` leaves out, or one `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` or `[MapCollection]` assigns, it would do nothing, and is reported (SMP0221).

A condition whose parameter does not take null (a reference annotated as not null, or `[DisallowNull]`) is not given a null source, which does not meet it: `if (source.Name is not null && IsShort(source.Name))`. A condition taking the struct a nullable struct source holds, or a type it converts to implicitly, gets its `Value`, and a null source does not meet it: `if (source.Count is not null && IsPositive(source.Count.Value))`.

### Auto-mapping disabled (`AutoMap = false`)

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]
public static partial void Map(Source source, Destination destination);
// Only 'Id' is mapped; other properties are ignored.
```

### Strict mode (`Strict = true`)

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

A destination property no mapping assigns is reported (SMP0501, a warning): one the mapper can assign through a setter, or through an `init` accessor for a return-type mapper, which sets it in the object initializer, that neither the automatic mapping nor an attribute maps, and `[MapIgnore]` does not leave out. A member a dotted target path writes into, such as `Child` for `[MapProperty("Child.Value", ...)]`, is mapped through the path. For a return-type mapper, so is a property only a constructor can set (get-only, or with a setter the mapper class cannot call) that a parameter of a constructor the mapper can call takes, when the construction chosen passes it no argument (see [Choosing the constructor](#choosing-the-constructor)), whether the source has it or not. A property no constructor takes, such as a computed one, is not reported, nor, for a void mapper, which never constructs, is one only a constructor sets or an `init`-only one. One an optional parameter left out would set is reported as well, the constructor giving it the default value; a `[MapIgnore]` naming it leaves it to that value without the warning. An `[Obsolete]` property is not reported (see [Obsolete members](#obsolete-members-obsolete)).

The warning is reported at the mapper method, as the errors about the construction of its destination are, so a `#pragma warning disable SMP0501` around the method suppresses it (see [Diagnostics](#diagnostics) for where each diagnostic is reported).

### Obsolete members (`[Obsolete]`)

The automatic mapping leaves out a property marked `[Obsolete]`, obsolete as a warning or as an error, as a source and as a destination: one marked itself, on the accessor it is read or assigned through, or on the property it overrides (C# reports the one the override overrides). Strict mode does not report it, while a `required` one still has to be mapped (SMP0303).

A member an attribute names is used when obsolete as a warning, which C# reports (CS0618), and reported with the diagnostic of the attribute when obsolete as an error, as the generated code could not use it (CS0619): a property or a field, a segment of a dotted path, and the method of `[MapFrom]` (SMP0213, SMP0204 or SMP0206 for a source, SMP0214 for a target). A method these attributes name that is obsolete as an error does not match either: a `Converter` (SMP0104), a `[MapCondition]` method (SMP0106), a `[MapUsing]` method (SMP0201), a `[BeforeMap]` / `[AfterMap]` callback (SMP0102 / SMP0103), and the mapper method of `[MapNested]` / `[MapCollection]` (SMP0211 / SMP0210).

A constructor obsolete as an error is never called, and one obsolete as a warning only when nothing else will do (see [Choosing the constructor](#choosing-the-constructor)). An intermediate member of a dotted target is created with `new T()` when the constructor that call binds to can be called: one obsolete as an error makes the type one that cannot be created, and the path writes into the member the destination holds, while one obsolete as a warning is called, warning, as nothing else creates the member.

The conversions the generated code makes on its own do not call a member obsolete as an error either: a conversion operator (`implicit` / `explicit`), a method of the `[ValueConverter]` / `[CollectionConverter]` class, the `ToString(format, provider)` and `Parse` of the conversions, and the constructor a collection class is created with. Another conversion takes over when there is one (the generic method of the converter class for a specialized one, the `Parse` taking a `string` for the one taking a span), and otherwise the value is reported as having no conversion (SMP0402), the converter method as not matching (SMP0104), or the collection as one the generated code cannot create (SMP0217). One obsolete as a warning is called, warning (CS0618).

An enum member marked `[Obsolete]`, as a warning or as an error, is written as a cast of its value, such as `(Color)2`, so that the generated code neither warns nor fails: in the switch converting an enum to another enum (the members still match by name) or to or from a string, and in an enum constant of `[MapConstant]` or `NullValue`, which is written as a member of the same value that is not obsolete when there is one.

### Assignment order (`Order`)

`[MapProperty]`, `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` and `[MapCollection]` take `Order`. Assignments of the same kind are emitted in ascending `Order` (0 by default), then in the order they are declared:

```csharp
[Mapper(AutoMap = false)]
[MapConstant(nameof(Destination.Label), "second", Order = 2)]
[MapConstant(nameof(Destination.Note), "first", Order = 1)]
public static partial void Map(Source source, Destination destination);
```

Generated code:

```csharp
destination.Note = "first";
destination.Label = "second";
```

The kinds follow a fixed sequence between `[BeforeMap]` and `[AfterMap]`: property mappings (auto-mapping and `[MapProperty]`, those guarded by a null check of a source path last), `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]`, then `[MapCollection]`. `Order` does not move an assignment across kinds.

---

## Nested Property Mapping

Use dot notation in `[MapProperty]` to flatten or unflatten nested properties.

### Flatten (nested source -> flat destination)

```csharp
[Mapper]
[MapProperty("ChildId",   "Child.Id")]
[MapProperty("ChildName", "Child.Name")]
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

When an intermediate member is null, a mapping with `NullValue` takes it, and the others leave their targets as they are, as do `NullBehavior.Skip` and a mapping a `[MapCondition]` guards, which has no source value to test:

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

### Unflatten (flat source -> nested destination)

```csharp
[Mapper]
[MapProperty("Child1.Value", "Value1")]
[MapProperty("Child2.Value", "Value2")]
public static partial void Map(Source source, Destination destination);
```

Intermediate destination objects are auto-instantiated when a value is assigned through them:

```csharp
destination.Child1 ??= new DestinationChild();
destination.Child2 ??= new DestinationChild();
destination.Child1.Value = source.Value1;
destination.Child2.Value = source.Value2;
```

An assignment made under a check of its own, which may leave the target as it is, creates them inside the check, right before it assigns: with `NullBehavior.Skip`, a `[MapCondition]`, a null check of an intermediate member of its source path, or a `Converter` not called for a null source. An intermediate member stays null when nothing is assigned through it, as in an update that leaves the members it is not given as they are; one an assignment without such a check goes through as well is created up front, and a `NullValue` taken when an intermediate member of the source path is null creates it in both cases. A return-type mapper creates them the same way, except in the object initializer:

```csharp
// [MapProperty("Address.City", nameof(Request.City), NullBehavior = NullBehavior.Skip)]
if (request.City is not null)
{
    customer.Address ??= new Address();
    customer.Address.City = request.City!;
}
```

An intermediate member the mapper cannot assign (get-only, `init`-only, or with a setter it cannot call), or whose type it cannot create (abstract, an interface, without a constructor it can call without arguments, or with required members), is not instantiated: the instance it holds is filled, and nothing is assigned when it is null:

```csharp
// Destination.Child: public DestinationChild Child { get; } = new();
if (destination.Child is not null)
{
    destination.Child.Value = source.Value1;
}
```

A struct property is a value, so it is copied into a local, filled, and assigned back; a struct field is filled directly. One that cannot be assigned back, a get-only property or a `readonly` field, is reported (SMP0214):

```csharp
// Destination.Point: public Point Point { get; set; } (a struct)
{
    var __copy0 = destination.Point;
    __copy0.X = source.Value1;
    destination.Point = __copy0;
}
```

An `init`-only member at the end of a path can only be set in an object initializer. A return-type mapper sets it there, creating the members it goes through, each of which has to be assignable there and creatable. A void mapper cannot (SMP0302), and a path the initializer cannot create either, through a get-only member for example, is reported (SMP0214):

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

The dotted targets of `[MapConstant]`, `[MapExpression]` and `[MapUsing]` go through their intermediate members the same way.

A dotted path into a member, of `[MapProperty]`, `[MapConstant]`, `[MapExpression]` or `[MapUsing]` alike, takes the place of the automatic mapping of that member, which is then not mapped as a whole: the path writes into the member the destination holds or creates, never into the object of the source. A member left out with `[MapIgnore]` still takes the dotted paths into it. A member mapped as a whole by an attribute cannot also take a dotted path (SMP0101), nor can a member the constructor of a return-type mapper assigns from an argument, such as a parameter of a positional `record`, as the path would write into the object passed to the constructor (SMP0222). A `required` member a return-type mapper sets is created in the object initializer when its type can be created, and the paths write into it after construction, an `init`-only member at the end in the initializer.

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

Generated code (for a `List<SourceChild>` source and a `List<DestinationChild>` target):

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

The loop is generated inline, shaped by the source and target collection types. A null source collection sets the target to `default`. The target gets the collection the loop builds: a `List<T>` for `List<T>` and its interfaces, an array, a `HashSet<T>` for sets, a `Dictionary<TKey, TValue>` for `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>` (the element mapper maps the `KeyValuePair<TKey, TValue>` pairs), or the immutable or frozen collection of its type (`ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>` and their interfaces, and `FrozenSet<T>`; the other immutable and frozen collections, such as `ImmutableDictionary<TKey, TValue>` or `FrozenDictionary<TKey, TValue>`, are reported (SMP0217) unless a collection converter builds them). A collection class the mapper can create, such as `ObservableCollection<T>` or `class ItemList : List<Item>`, is built with its own constructor and filled through `ICollection<T>`; one it cannot create is reported (SMP0217) unless a collection converter builds it. A void element mapper `(SourceChild, DestinationChild)` fills a `new DestinationChild()`, so the element type has to be creatable with `new()` (SMP0210 otherwise). The collection created keeps the nullable annotations of the elements of the target (`List<DestinationChild?>`), and the result of a mapper declared to take null, as in `DestinationChild? MapChild(SourceChild? source)`, which returns null only for a null source, is taken with `!` for elements not annotated as nullable, as it is for the target of `[MapNested]`. The element mapper matches by the types the way the mapper of `[MapNested]` does: a nullable struct element goes to a mapper taking the struct as the value it holds, and a null one gives `default`. A nullable reference element goes to a mapper whose parameter does not take null the same way, when it has a value, and a null one gives `default` (`__src[__i] is { } __value ? MapChild(__value) : default!`); a collection converter, which takes the mapper as a delegate, gets every element.

A collection class of its own is a collection by the `IEnumerable<T>` it implements through its base type or interfaces, on the source as well as on the target:

```csharp
public class SourceChildList : List<SourceChild> { }
public class DestinationChildList : List<DestinationChild> { }

// Source.Children: SourceChildList / Destination.Children: DestinationChildList
// Generated:
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

With a collection converter (`[CollectionConverter]`, see below), its method is called instead, as in `CustomCollectionConverter.ToList<SourceChild, DestinationChild>(source.Children, MapChild)!`. `Converter` on `[MapCollection]` names the method to call, on the `[CollectionConverter]` type or, without one, on `DefaultCollectionConverter`, which provides such methods (`ToList`, `ToArray`, `ToHashSet`, `ToImmutableArray`, ...) for both function-mapper and action-mapper variants.

### Refilling the existing collection (`Strategy = CollectionStrategy.InPlace`)

By default the target gets a new collection. `CollectionStrategy.InPlace` keeps the target instance, clears it and adds the mapped elements, which preserves a reference held elsewhere:

```csharp
[Mapper(AutoMap = false)]
[MapCollection(nameof(Destination.Children), Mapper = nameof(MapChild), Strategy = CollectionStrategy.InPlace)]
public static partial void Map(Source source, Destination destination);
```

Generated code (for a `List<SourceChild>` source and a `List<DestinationChild>` target):

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

The target is cleared and refilled through `ICollection<T>`, so its declared type has to implement it without being read-only by design: `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `IEnumerable<T>`, arrays, the immutable and frozen collections and `ReadOnlyCollection<T>` are reported (SMP0219). When the target is null:

- A property the mapper can assign gets a new instance of its type (such as `new ObservableCollection<T>()`), or a `List<T>` for an interface (a `HashSet<T>` for `ISet<T>`, a `Dictionary<TKey, TValue>` for `IDictionary<TKey, TValue>`); a type that is neither is reported (SMP0217).
- A property it cannot assign (get-only, a `private` or `init` setter) is left null; the instance it holds is refilled otherwise:

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

An instance that is read-only at run time behind a type such as `IList<T>` (an array, for example) throws `NotSupportedException` from `Clear`; the target keeps its instance, as `InPlace` promises, rather than being replaced behind the caller's back. `InPlace` always emits the loop; a collection converter is not used. A null source leaves the target as it is, neither cleared nor replaced. A `required` member of the destination a return-type mapper creates has to be set before construction, where there is no instance to refill, and is reported (SMP0219) unless the constructor called has `[SetsRequiredMembers]`; an `init`-only member is refilled in the instance it holds, as a get-only one is.

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

The mapper takes the source member as its type, or as one the member converts to by an implicit reference conversion (a base class or an interface, taken by value), and returns the target's type, one converting to it the same way (such as a class for a target of an interface it implements), or the struct of a nullable struct target. A nullable struct member goes to a mapper taking the struct as the value it holds, after the null check, and a null one gives `default`, as a null reference does: `source.Point is not null ? MapPoint(source.Point.Value) : default!`. A void mapper takes the instance created for the target as its type, or, by value, as one it converts to. A mapper the source or the target does not convert to this way does not match (SMP0211). Of the overloads, the one the call binds to is used, as C# binds it: a call binding to a method that does not match (a more specific one returning another type, a generic one, one with optional parameters, or one obsolete as an error) is reported (SMP0211, and SMP0210 for the element mapper of `[MapCollection]`).

For an `init`-only target, or a `required` one the constructor called does not set, a return-type mapper makes the value before construction and sets it in the object initializer, as `[MapCollection]` does (see [record / Primary Constructor Support](#record--primary-constructor-support)).

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
    var __d = new DestModel(src.Id, src.Name);
    return __d;
}
```

A void mapper never constructs, so the constructors of the destination do not affect it: a member only a constructor assigns is left out of the automatic mapping, like any get-only property.

> A `void` mapper cannot assign `init`-only members, such as the properties of a positional `record`, nor a member only a constructor assigns named by `[MapProperty]` (SMP0302).

A dotted target into a member the constructor assigns, such as `[MapProperty("Child.Value", ...)]` for `record Dst(Child Child)`, is reported (SMP0222): it would write into the object passed to the constructor, which is the source's own when the argument copies it.

A member the constructor assigns can take the value of `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` or `[MapCollection]`, which goes to the argument (`new Dst(Build(src))`). `[MapNested]` and `[MapCollection]` make it before construction; `InPlace` has no instance to refill there (SMP0219).

They make the value of an `init`-only member, and of a `required` one the constructor called does not set, before construction as well, and the object initializer sets it, with the null handling, the element annotations and the mappers as for any other target; a void mapper cannot assign an `init`-only member (SMP0212):

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

The `required` members of the destination, properties and fields of any accessibility and those of its base classes, are set in the object initializer of a return-type mapper, so each has to be mapped (SMP0303) and cannot be ignored (SMP0216). The automatic mapping and `[MapProperty]` go through the public properties, while `[MapConstant]`, `[MapExpression]` and `[MapUsing]` also take an `internal` member. A required member the dotted paths write into is created in the object initializer when its type can be created. A required member a parameter of the constructor called assigns is reported (SMP0304): the object initializer would have to set it again, replacing what the constructor made of the argument, so the constructor needs `[SetsRequiredMembers]`. When the constructor called has `[SetsRequiredMembers]`, they are not required: an unmapped one keeps what the constructor set, the mapped ones are still set in the object initializer, and `[MapNested]` / `[MapCollection]` can assign one after construction. A void mapper fills an instance that already exists, so they do not concern it.

### Choosing the constructor

The constructor a return-type mapper calls is chosen by these rules:

- The candidates are the constructors declared with parameters that the mapper class can call, taking every argument as a value. A `private` or `protected` one it cannot call, one obsolete as an error, and one with a `ref`, `out` or `ref readonly` parameter are not candidates (an `in` parameter takes a value as well). An abstract class has none.
- A value an attribute gives to a target only a constructor receives, a parameter no member has or a member without a setter the mapper class can call, chooses the constructor: of the candidates the mapping gives every argument, the one receiving the most of these targets is called, one not obsolete as a warning over one that is, then the longest, then the first declared. A constructor obsolete as a warning alone receiving them is called, warning (CS0618). A target a setter receives does not choose one, so a type whose setters the mapper can all call constructs as before. A target no such candidate receives is reported (SMP0214).
- Short of that, a constructor obsolete as a warning (`[Obsolete]`, CS0618) is called only when nothing else constructs the destination: while another candidate the mapping gives every argument, or a constructor callable without arguments, is there, it is not chosen, and a parameterless one obsolete as a warning does not count as a way to construct without arguments.
- Otherwise, the longest candidate decides whether construction takes arguments: it does when the type is a `record`, when a parameter of it has no matching property the mapper can assign after construction (none, a get-only or `init`-only one, or one whose setter the mapper class cannot call, such as a `private set`), or when no public parameterless constructor exists. Otherwise the generator emits `new Dst()` plus property assignments, with init-only members assigned in the object initializer.
- The one called is the longest candidate the mapping has a value for every parameter of: a source property matching the parameter or the member it assigns, or an attribute naming either. A parameter whose member, or itself, `[MapIgnore]` names has none. An optional parameter (with a default value, `[Optional]` or `params`) without a value is left out to take its default, and the arguments after it are passed by name (`new Dst(src.A, c: src.C)`); a candidate whose call, with the parameters left out, another constructor takes as well (the same types, its other parameters optional) is passed over, as the call would bind to that one or be ambiguous. When that candidate needs no arguments either (every parameter matches a property the mapper can assign, and a public parameterless constructor exists), `new Dst()` is emitted.
- When no candidate gets every argument, a destination that can be created without arguments is created with `new Dst()`, and the members only a constructor assigns are not mapped. One that cannot binds the longest candidate, and its parameter without a value is reported (SMP0301, or SMP0216 for one `[MapIgnore]` names).
- A destination that cannot be created at all, an abstract class, an interface, a type with neither a candidate nor a constructor the mapper class can call without arguments, or a type parameter without the `new()` or the `struct` constraint, is reported (SMP0305).

The rules apply in this order: a constructor obsolete as an error is not a candidate, the targets of the attributes choose, one obsolete as a warning is avoided, then the longest, then the first declared.

```csharp
public class Order
{
    public Order() { }
    public Order(int id, string name) { Id = id; Name = name; }

    public int Id { get; private set; }
    public string Name { get; set; } = "";
}

[Mapper]
public static partial Order Map(OrderSource src);   // new Order(src.Id, src.Name), or new Order() when OrderSource has no Id
```

### Conversion of constructor arguments

Constructor arguments go through the same conversion pipeline as ordinary property assignments, so type conversion, `Converter`, `NullValue` and `Culture` / format settings all apply. The same holds for `init`-only members assigned in the object initializer. An argument is checked and converted for the type of the parameter, which need not be the type of the member it assigns: a `string` parameter for an `int` property takes the conversion to `string`, as a `string` property would. The values of `Converter`, `NullValue` and the attributes are checked by the rules of a property of the parameter's type as well: a converter or a method returning the member's type is reported for a parameter of another type (SMP0105, SMP0202, SMP0205, SMP0211, SMP0218), as it would be for such a property.

```csharp
public class Src { public int? Value { get; set; } }
public record Dst(string Value);

[Mapper]
public static partial Dst Map(Src src);
```

Generated code:

```csharp
var __d = new Dst(src.Value is not null
    ? DefaultValueConverter.ConvertToString(src.Value.GetValueOrDefault())
    : default!);
```

When a nullable source is null, the argument falls back to the destination type's `default` — or to `NullValue` when one is specified, or to `null` when the target is nullable. A nullable intermediate segment in a dotted source path is guarded the same way (`src.Child is not null ? ... : default!`).

Statement-only options cannot apply to a constructor argument: `[MapCondition]` has no way to leave the member unassigned, and `NullBehavior.Skip` has no previous value to keep. Both are rejected with `SMP0215`.

A get-only property assigned through a constructor can also be remapped:

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

A constructor parameter with no matching destination property is supported the same way. Target it with `[MapProperty]` using the parameter name to remap it:

```csharp
public class Src { public int Other { get; set; } }

public class Dst
{
    public Dst(string value) { Text = value; }
    public string Text { get; }
}

[Mapper]
[MapProperty("value", nameof(Src.Other))]   // "value" is the constructor parameter name
public static partial Dst Map(Src src);
```

`[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` and `[MapCollection]` can target such a parameter by its name as well. A `[MapProperty]` naming a parameter by its own name, where a member of another spelling matches it too (the parameter `value` and the property `Value` under the `Ordinal` comparison), is what the argument takes.

---

## Null Handling

| Source type | Destination type | Behavior |
|-------------|-----------------|----------|
| `T?` | `T?` | Copied as-is (including null) |
| `T?` | `T` (leaf) | `default!` assigned when null |
| `T` | `T?` | Copied as-is |
| `T` | `T` | Copied as-is |

Nullable intermediate paths on the **source side**, of `[MapProperty]` and `[MapFrom]` alike, are guarded with `if (... is not null)`; when one is null, a mapping with `NullValue` takes it, and the others leave their targets as they are.
Nullable intermediate paths on the **destination side** are auto-instantiated with `??= new` when the mapper can assign and create them; otherwise the instance they hold is filled.

A source parameter (or the destination parameter of a void mapper) declared nullable, as in `Map(Src? source)`, is checked before anything is mapped. When it is null nothing is mapped: a return-type mapper returns `default`, and a void mapper returns without touching the destination.

A return type declared as a nullable struct, as in `Point? Map(Src source)`, is created and filled as the struct it holds. A source parameter declared as one, as in `Map(Point? source)`, is reported (SMP0006), as it has none of the members of the struct it holds: take the struct, and check null before the call.

---

## Type Conversion

Same-type and implicitly convertible assignments are generated without a converter: a numeric widening, a value to its nullable type, and an implicit reference conversion, through variance as well (`IReadOnlyList<Circle>` to `IReadOnlyList<Shape>`, `Circle[]` to `Shape[]`). The same enum on both sides is copied as it is, which keeps a value no member has, such as a combination of flags.
When a conversion is needed, the specialized method of `DefaultValueConverter` for the types is called, as below. Other types are converted by their own conversion operator, `Parse` (from a string, to a type implementing `IParsable<T>`) or `ToString(format, provider)` (to a string); a number goes to a narrower numeric type by a cast, as C# casts it (`(int)source.LongValue`), and an enum by a switch over its members, or by a cast to or from a number. A conversion none of these makes is reported (SMP0402), unless a `[ValueConverter]` class takes it (see below).

An enum going to another enum matches the members by name. A value no member of the target has the name of, such as a combination of flags, gives `default`, or `null` to a nullable enum target. A string goes to an enum the same way, a string no member has the name of going through `Enum.Parse`, which throws for one it cannot parse, or to a nullable enum target through `Enum.TryParse`, which gives `null` for it (a number in the string still gives its value).

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
    ? DefaultValueConverter.ConvertToString(source.NullableValue.GetValueOrDefault())
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

The converter class may be nested or generic (`typeof(Outer.CustomConverter)`, `typeof(CustomConverter<TMarker>)`). `Method` changes the name its methods are looked up by (default `"Convert"`): the specialized methods become `{Method}To{TargetType}`, and the generic fallback `{Method}<TSource, TDestination>`:

```csharp
public static class MapConverter
{
    public static string MapToString(int source) => $"ID_{source}";
    public static TDestination Map<TSource, TDestination>(TSource source) { ... }
}

[Mapper]
[ValueConverter(typeof(MapConverter), Method = "Map")]
public static partial void Map(Source source, Destination destination);

// Generated: destination.Value = MapConverter.MapToString(source.Value);
```

A converter method that is missing, such as the generic fallback of a conversion no specialized method covers, is reported (SMP0104).

Priority order (highest to lowest):

| Level | Scope |
|-------|-------|
| `[MapProperty(Converter = nameof(...))]` | Single property |
| `[ValueConverter]` on mapper method | All properties of that method |
| `[ValueConverter]` on class | All mapper methods in the class |
| `DefaultValueConverter` | Fallback |

The `Converter` of `[MapProperty]` takes the source member as the method of `[MapUsing]` takes the source, also as a type it converts to implicitly, or as the struct a nullable struct holds (see Static method calculation). It returns the target type, or a type converting to it implicitly as the method of `[MapUsing]` does (SMP0105 otherwise), and a nullable reference it returns into a target not annotated as nullable is taken with `!`.

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

The method is picked by the target type (`ToList`, `ToArray`, `ToHashSet`, `ToImmutableArray`, ...) unless `Converter` of `[MapCollection]` names one, and is called as `Method<TSourceElement, TTargetElement>(source, mapper)`. A method that is missing, does not take the source collection, or returns something the target property cannot take is reported (SMP0104).

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

Priority: `[MapProperty]` > `[Mapper]` > `[MapperProfile]` > `CultureInfo.InvariantCulture`

`Culture`, `DateTimeFormat` and `NumberFormat` are each resolved on their own. Under `[MapperProfile(Culture = "ja-JP", NumberFormat = "N0")]`, `[Mapper(NumberFormat = "N2")]` formats with ja-JP and N2, and `[Mapper(Culture = "de-DE")]` with de-DE and N0.

With a culture, the specialized methods of the converter are called through their overload taking the culture and the format, as in `DefaultValueConverter.ConvertToString(int source, IFormatProvider culture, string? format)`. A custom `[ValueConverter]` has to provide that overload for each specialized method it uses (SMP0104 otherwise).

The resolved `CultureInfo` is cached as a `static readonly` field in the generated class to avoid repeated `GetCultureInfo(...)` calls.

> Specifying `DateTimeFormat` / `NumberFormat` without `Culture` is a compile-time error (SMP0401).

> The `DateTimeFormat` of `[Mapper]` and `[MapperProfile]` applies to every date and time type the mapper converts, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan` alike. To format them differently, give `DateTimeFormat` on `[MapProperty]` for each. A `TimeSpan` format is written differently from the others (`hh\:mm`, with the separators escaped), and one meant for dates fails for a `TimeSpan` or a `TimeOnly` at run time (`FormatException`).

> `Culture` has to be a culture name: subtags of letters and digits separated by hyphens, such as `ja-JP` or `zh-Hant-TW`, with an alternate sort order after an underscore, such as `de-DE_phoneb` (SMP0404 otherwise).

---

## NativeAOT / Trimming

Smart.Mapper is fully compatible with NativeAOT and IL trimming.

- `<IsAotCompatible>true</IsAotCompatible>` is declared in `Smart.Mapper.csproj`
- All type conversions are handled through specialized methods - no generic reflection fallback at runtime
- The generated code never uses `Activator.CreateInstance`; object creation is expanded inline by the generator (the `Action` overloads of `DefaultCollectionConverter`, which create elements with `new()`, are marked `RequiresDynamicCode`)
- `[DynamicallyAccessedMembers]` annotations are applied to `ValueConverterAttribute.ConverterType` and `CollectionConverterAttribute.ConverterType`

> **`[MapExpression]` warning** - If an expression contains reflection APIs (`Activator`, `Type.GetType`, `MethodInfo`, etc.), SMP0403 is emitted. Prefer `[MapFrom]` or `[MapUsing]` in AOT contexts.

---

## Diagnostics

A diagnostic whose cause is an attribute is reported at that attribute: the second of two attributes that contradict each other (SMP0101), and the `[MapperProfile]` or `[ValueConverter]` of the class for a value it gives. One about the method itself, the automatic mapping, the construction of the destination (SMP0301, SMP0303, SMP0304, SMP0305) or strict mode (SMP0501) is reported at the mapper method.

| ID | Description | Severity |
|----|-------------|----------|
| SMP0001 | Mapper method must be `static partial`, in types that are all `partial` | Error |
| SMP0002 | Mapper method has an invalid number of parameters | Error |
| SMP0003 | Two custom parameters have the same type | Error |
| SMP0004 | Mapper parameter name starts with `__` (reserved for the generated code) | Error |
| SMP0005 | Parameter has a modifier the generated code cannot work with (`out`, or none, `in` or `ref readonly` on the struct destination of a void mapper, which is taken by `ref`) | Error |
| SMP0006 | Source parameter is a nullable value type, which has none of the members of the struct it holds | Error |
| SMP0101 | Several mapping attributes target the same destination property or a member and a member of it (`Child` and `Child.Value`), or `[MapIgnore]` and a mapping attribute name the same target | Error |
| SMP0102 | `BeforeMap` method signature does not match | Error |
| SMP0103 | `AfterMap` method signature does not match | Error |
| SMP0104 | Converter method is not found or its signature does not match | Error |
| SMP0105 | Converter return type does not convert implicitly to the target property type | Error |
| SMP0106 | Property condition method signature does not match | Error |
| SMP0201 | `MapUsing` method signature does not match | Error |
| SMP0202 | `MapUsing` return type does not convert implicitly to the target property type | Error |
| SMP0203 | `[MapFrom]` target property is not found on the destination type | Error |
| SMP0204 | `MapFrom` member is not a parameterless method the mapper can call or a property path of the source type (inherited ones included) | Error |
| SMP0205 | `MapFrom` member type does not convert implicitly to the target property type | Error |
| SMP0206 | `[MapCollection]` / `[MapNested]` source property is not found | Error |
| SMP0207 | `[MapCollection]` / `[MapNested]` target property is not found | Error |
| SMP0208 | `[MapCollection]` source property is not a collection type | Error |
| SMP0209 | `[MapCollection]` target property is not a collection type | Error |
| SMP0210 | `MapCollection` element mapper method is not found or its signature does not match | Error |
| SMP0211 | `MapNested` mapper method is not found or its signature does not match | Error |
| SMP0212 | `[MapCollection]` / `[MapNested]` target cannot be assigned (no setter or `init` accessor the mapper can call, or `init`-only in a void mapper; `InPlace` refills the instance it holds) | Error |
| SMP0213 | `[MapProperty]` source property is not found | Error |
| SMP0214 | Mapping target is not found or cannot be assigned (no setter the mapper can call, a `readonly` field, a dotted path the generated code cannot go through), including the target of `[MapIgnore]` / `[MapCondition]` | Error |
| SMP0215 | `[MapCondition]` / `NullBehavior.Skip` on a target assigned through a constructor or object initializer | Error |
| SMP0216 | `[MapIgnore]` on a member assigned through a constructor the destination cannot be created without, or on a `required` member of the destination a return-type mapper creates | Error |
| SMP0217 | `[MapCollection]` target cannot take the collection the generated code creates for it | Error |
| SMP0218 | `[MapConstant]` value or `NullValue` cannot be assigned to the target type, or puts `null` where the target does not take it | Error |
| SMP0219 | `InPlace` target cannot be cleared and refilled (no `ICollection<T>`, read-only by design, or a member the constructor takes or a `required` one, without an instance before construction) | Error |
| SMP0220 | `[MapConstant]` value or `NullValue` cannot be written in the generated code (such as a file-local type) | Error |
| SMP0221 | `[MapCondition]` target has no property mapping for the condition to guard | Error |
| SMP0222 | Dotted target goes into a member the constructor of a return-type mapper assigns from an argument | Error |
| SMP0223 | `[MapIgnore]` target is a dotted path (a member of a member), which the automatic mapping never assigns on its own | Error |
| SMP0301 | A parameter of the constructor the destination cannot be created without has no value: no matching source property or attribute, and it is not optional | Error |
| SMP0302 | A `void` mapper cannot assign `init`-only members (such as the properties of a positional `record`, or one at the end of a dotted path) or members only a constructor assigns | Error |
| SMP0303 | `required` member (property or field, of any accessibility, inherited ones included) of the destination a return-type mapper creates is not mapped (unless its constructor has `[SetsRequiredMembers]`) | Error |
| SMP0304 | A constructor argument assigns a `required` member, which the object initializer would have to set again (unless the constructor has `[SetsRequiredMembers]`) | Error |
| SMP0305 | Return-type mapper cannot create the destination (abstract, an interface, without a constructor the mapper can call with values, or a type parameter without the `new()` / `struct` constraint) | Error |
| SMP0401 | `DateTimeFormat` / `NumberFormat` is specified without a `Culture` that applies | Error |
| SMP0402 | Not AOT-safe: the conversion may fall back to the generic `Convert<TSource, TDestination>` (for a class, a struct or a collection, the message tells to use `[MapNested]` / `[MapCollection]`) | Error |
| SMP0403 | AOT warning: `MapExpression` may contain a reflection pattern | Warning |
| SMP0404 | `Culture` is not a culture name | Error |
| SMP0501 | Strict mode: a destination property the mapper can assign, or one only a constructor the mapper can call sets, is not mapped (reported at the mapper method) | Warning |

See [Diagnostics.md](Diagnostics.md) for the cause of each diagnostic and how to fix it.

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
# Run all tests
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj

# Run with code coverage (Cobertura XML under TestResults in the output directory)
dotnet run --project Smart.Mapper.Tests/Smart.Mapper.Tests.csproj -- --coverage --coverage-settings CodeCoverage.runsettings
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

- **Direct `FrozenSet` construction** — the generated code builds a `HashSet<T>` and calls `ToFrozenSet` (two-phase by BCL design). If the BCL ever ships a frozen-collection builder API, the intermediate set can be eliminated.
- **Generic fallback `Convert<TSource, TDestination>` for `Half` / `Int128` / `UInt128` / `BigInteger` sources** — these currently reach the boxing fallback when routed through the generic converter opt-in; specialized branches can be added if demand arises (the default specialized-method path already covers them).
- **Generator incrementality tuning** — output is regenerated per run via `Collect()` and destination/source property walks are repeated per feature pass. Measured cost is negligible today; revisit (per-class output splitting, property-list caching) if very large models appear.

---

## License

MIT
