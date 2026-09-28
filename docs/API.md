# Smart.Mapper API Reference

[README](../README.md) | [Diagnostics](../Diagnostics.md) | [日本語](API.ja.md)

This document describes the attributes of Smart.Mapper and the rules the source generator follows, edge cases included. For an overview and short examples, see the [README](../README.md); for the cause of each diagnostic and how to fix it, see [Diagnostics.md](../Diagnostics.md).

## Contents

- [Mapper methods](#mapper-methods)
  - [Static and instance mappers](#static-and-instance-mappers) · [Void and return patterns](#void-and-return-patterns) · [Parameters](#parameters) · [Custom parameters](#custom-parameters) · [Extension methods](#extension-methods) · [Mappers in nested types](#mappers-in-nested-types) · [Generic mapper methods](#generic-mapper-methods) · [What a mapper maps](#what-a-mapper-maps)
- [Attribute reference](#attribute-reference)
  - [Attribute overview](#attribute-overview) · [Names in attributes](#names-in-attributes) · [`[Mapper]`](#mapper) · [`[MapperProfile]`](#mapperprofile) · [`[MapProperty]`](#mapproperty--mappropertyt) · [`[MapIgnore]`](#mapignore) · [`[MapUsing]`](#mapusing) · [`[MapFrom]`](#mapfrom) · [`[MapConstant]`](#mapconstant--mapconstantt) · [`[MapExpression]`](#mapexpression) · [`[MapCondition]`](#mapcondition) · [`[BeforeMap]` / `[AfterMap]`](#beforemap--aftermap) · [`[MapNested]`](#mapnested) · [`[MapCollection]`](#mapcollection) · [`[ValueConverter]`](#valueconverter) · [`[CollectionConverter]`](#collectionconverter) · [Enums](#enums)
- [Auto-mapping and name matching](#auto-mapping-and-name-matching)
  - [Auto-mapping](#auto-mapping) · [Disabling auto-mapping](#disabling-auto-mapping) · [Name comparison](#name-comparison) · [Assignment order](#assignment-order) · [Obsolete members](#obsolete-members)
- [Methods named by attributes](#methods-named-by-attributes)
  - [Lookup](#lookup) · [Instance and static methods](#instance-and-static-methods) · [Taking the value](#taking-the-value) · [Overloads](#overloads) · [Return values](#return-values)
- [Property paths](#property-paths)
  - [Flatten](#flatten) · [Unflatten](#unflatten) · [Dotted paths and the automatic mapping](#dotted-paths-and-the-automatic-mapping)
- [Nested objects](#nested-objects)
- [Collections](#collections)
  - [Mapping the elements](#mapping-the-elements) · [Target collections](#target-collections) · [Element mappers](#element-mappers) · [Collection classes](#collection-classes) · [Collections as a whole](#collections-as-a-whole) · [Collection converters](#collection-converters) · [Refilling the existing collection](#refilling-the-existing-collection)
- [Constructors and records](#constructors-and-records)
  - [Constructor calls](#constructor-calls) · [Void mappers and constructors](#void-mappers-and-constructors) · [Values for constructor parameters](#values-for-constructor-parameters) · [`init`-only and `required` members](#init-only-and-required-members) · [Choosing the constructor](#choosing-the-constructor) · [Conversion of constructor arguments](#conversion-of-constructor-arguments)
- [Null handling](#null-handling)
  - [Nullable values](#nullable-values) · [Null substitution (`NullValue`)](#null-substitution-nullvalue) · [Keeping the destination value (`NullBehavior.Skip`)](#keeping-the-destination-value-nullbehaviorskip) · [Nullable annotations disabled](#nullable-annotations-disabled) · [`[MaybeNull]` members](#maybenull-members) · [Nullable parameters and return types](#nullable-parameters-and-return-types) · [Nullable annotations of created instances](#nullable-annotations-of-created-instances)
- [Type conversion](#type-conversion)
  - [Direct assignments](#direct-assignments) · [Specialized methods](#specialized-methods) · [Other conversions](#other-conversions) · [Enum conversions](#enum-conversions) · [Default formats](#default-formats) · [`DefaultValueConverter`](#defaultvalueconverter) · [Custom value converters](#custom-value-converters) · [Converter priority](#converter-priority)
- [Culture and formats](#culture-and-formats)
  - [Default culture](#default-culture) · [Culture names](#culture-names) · [`CultureInfo` parameter](#cultureinfo-parameter) · [Culture precedence](#culture-precedence) · [Formats](#formats) · [Converter overloads taking the culture](#converter-overloads-taking-the-culture)
- [Profiles](#profiles)
- [Callbacks and conditions](#callbacks-and-conditions)
  - [Before and after callbacks](#before-and-after-callbacks) · [Conditional mapping](#conditional-mapping)
- [Strict mode](#strict-mode)
  - [Unmapped members](#unmapped-members) · [Values that may be null](#values-that-may-be-null) · [Enum members without a match](#enum-members-without-a-match) · [Suppressing the warnings](#suppressing-the-warnings)
- [NativeAOT and trimming](#nativeaot-and-trimming)
- [Diagnostics](#diagnostics)
  - [Where diagnostics are reported](#where-diagnostics-are-reported) · [Diagnostic list](#diagnostic-list)

---

## Mapper methods

A mapper method is a `partial` method marked with `[Mapper]`, whose implementation the generator writes at compile time. It maps the members of the source to those of the destination: a void mapper fills a destination it is given, and a return-type mapper creates the destination and returns it.

### Static and instance mappers

`[Mapper]` can be put on a static or an instance (non-static) partial method. The types containing it have to be all `partial`, and none of them file-local (`file`), as the generated code declares them again in a file of its own (SMP0001 otherwise, see [Mappers in nested types](#mappers-in-nested-types)).

```csharp
// Static mapper
internal static partial class ObjectMapper
{
    [Mapper]
    public static partial Destination Map(Source source);
}

// Instance mapper, using a service injected through the constructor
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

An instance mapper calls the methods its attributes name on itself, so they may be instance methods of the mapper class or its base classes as well as static ones: the methods of `[BeforeMap]`, `[AfterMap]`, `[MapUsing]` and `[MapCondition]`, the `Converter` of `[MapProperty]`, and the `Mapper` of `[MapNested]` / `[MapCollection]`. The methods of a class containing the mapper class and of the types a `global using static` directive imports have to be static. A `[MapExpression]` of an instance mapper in a class can use the instance members as well, but not one in a struct, where C# does not let a local function use `this` (CS1673); use a `[MapUsing]` instance method there instead. A static mapper has no instance to call an instance method on, so naming one is reported (SMP0107). See [Instance and static methods](#instance-and-static-methods).

An extension method mapper stays static, as extension methods are ([Extension methods](#extension-methods)).

The implementation repeats the modifiers of the declaration: `static` for a static mapper, its accessibility, `new` and `unsafe`. A void mapper may be declared without an accessibility modifier, as in `static partial void Map(Source source, Destination destination);` (implicitly `private`), and its implementation is declared the same way.

### Void and return patterns

```csharp
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

Generated code (void pattern):

```csharp
public static partial void Map(Source source, Destination destination)
{
    destination.Id          = source.Id;
    destination.Name        = source.Name;
    destination.Description = source.Description;
}
```

Generated code (return pattern):

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

A void mapper fills the destination it is given and never constructs one, so the constructors of the destination do not affect it ([Void mappers and constructors](#void-mappers-and-constructors)). A struct destination is taken by `ref`, as in `Map(Source source, ref Destination destination)`: passed by value, it would be a copy the caller never sees filled, and passed as `in` or `ref readonly`, its members could not be assigned (SMP0005).

A return-type mapper creates the destination with the constructor chosen as described in [Constructors and records](#constructors-and-records), and returns it by value: one declared to return by `ref` or `ref readonly` is reported (SMP0008), as the reference would have to point to the destination it creates. A return type declared as a nullable struct, as in `Point? Map(Src source)`, is created and filled as the struct it holds.

### Parameters

The first parameter of a mapper is the source. A void mapper takes the destination after it, and the parameters after these are [custom parameters](#custom-parameters). A mapper without a parameter, or a void mapper without a destination parameter after the source, is reported (SMP0002).

- A parameter name starting with `__` is reserved for the names the generated code declares (SMP0004).
- A parameter cannot be `out`, and the struct destination of a void mapper is taken by `ref`, not by value, `in` or `ref readonly` (SMP0005).
- A source parameter of a nullable value type, as in `Map(Point? source)`, has none of the members of the struct it holds (only `HasValue` and `Value`), so nothing would be mapped (SMP0006): take the struct itself, and check null before the call.
- Of several `CultureInfo` custom parameters, the one named `culture` gives the culture of the conversions, and a mapper with several and none of that name is reported (SMP0406; see [`CultureInfo` parameter](#cultureinfo-parameter)).

A source parameter, or the destination parameter of a void mapper, that may be null is checked before anything is mapped ([Nullable parameters and return types](#nullable-parameters-and-return-types)).

### Custom parameters

The parameters after the source (and the destination of a void mapper) are custom parameters. They are passed on to the methods the attributes name that declare them: the method of `[MapUsing]`, the `Converter` of `[MapProperty]`, the method of `[MapCondition]`, the `[BeforeMap]` / `[AfterMap]` callbacks, and the mappers of `[MapNested]` / `[MapCollection]`. A `[MapExpression]` can refer to them by name.

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial Destination Map(Source source, FormattingContext context);

private static string CombineFullName(Source source, FormattingContext context)
    => $"{source.FirstName}{context.Separator}{source.LastName}";
```

A method takes the custom parameters it declares after its usual parameters, any of them and in any order: each of its parameters takes the custom parameter of its type, or, when the mapper or the method has several parameters of that type, the one of its name. A method with a parameter that takes none of them (the mapper has no custom parameter of its type, or none of its name among several) does not match, and of the overloads, one taking more of the custom parameters goes before one taking fewer. A mapper may have several custom parameters of the same type. The element mapper handed to a collection converter as a delegate ([Collection converters](#collection-converters)) and the methods of `[ValueConverter]` / `[CollectionConverter]` classes do not receive them. The custom parameters are passed on as they are, without a null check.

```csharp
[Mapper]
[MapProperty(nameof(Destination.Title), Converter = nameof(Enclose))]
public static partial Destination Map(Source source, string open, string close);

// Takes both strings by name, in its own order
private static string Enclose(string value, string close, string open) => open + value + close;
```

A custom parameter of type `System.Globalization.CultureInfo` gives the culture of the conversions of the method as well ([`CultureInfo` parameter](#cultureinfo-parameter)).

### Extension methods

A `[Mapper]` method can be declared as an extension method, which is static. The generated implementation keeps the `this` modifier, so the mapper reads naturally at the call site.

```csharp
public static partial class ObjectMapper
{
    [Mapper]
    public static partial Destination ToDestination(this Source source);
}

var destination = source.ToDestination();
```

### Mappers in nested types

A `[Mapper]` method may be declared in a nested type. The generated code declares the containing types again, outermost first, with their kinds (`class`, `struct`, `record`, `record struct`) and type parameters, so each of them has to be `partial`, and none of them file-local (`file`), which another file cannot declare (SMP0001 otherwise).

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

### What a mapper maps

A mapper maps an object to another. One whose source or destination is a collection, an array or a tuple, or a type parameter constrained to one, is reported (SMP0007), as it would map the members of the collection and none of its elements: map the elements with a mapper of the element type, or a type holding the collection with `[MapCollection]` ([Collections as a whole](#collections-as-a-whole)).

---

## Attribute reference

### Attribute overview

| Attribute | Applies to | Description |
|-----------|------------|-------------|
| [`[Mapper]`](#mapper) | Method | Marks a mapper method; `AutoMap`, `Strict`, `NameComparison`, `Culture`, `DateTimeFormat`, `NumberFormat` |
| [`[MapperProfile]`](#mapperprofile) | Assembly, class, struct | Defaults of the mapper methods: `Strict`, `NameComparison`, `DefaultCulture`, `Culture`, `DateTimeFormat`, `NumberFormat` |
| [`[MapProperty]`](#mapproperty--mappropertyt) / `[MapProperty<T>]` | Method, multiple | Maps a target from a source member or a dotted path; `Converter`, `NullValue`, `NullBehavior`, `Culture`, `DateTimeFormat`, `NumberFormat`, `Order` |
| [`[MapIgnore]`](#mapignore) | Method, multiple | Leaves a destination member out of the automatic mapping |
| [`[MapUsing]`](#mapusing) | Method, multiple | Assigns the value a method computes from the source (custom-parameter aware) |
| [`[MapFrom]`](#mapfrom) | Method, multiple | Assigns the result of a parameterless method of the source, or a property path of it |
| [`[MapConstant]`](#mapconstant--mapconstantt) / `[MapConstant<T>]` | Method, multiple | Assigns a constant value |
| [`[MapExpression]`](#mapexpression) | Method, multiple | Assigns the value of a C# expression (e.g. `"System.DateTime.Now"`) |
| [`[MapCondition]`](#mapcondition) | Method, multiple | Maps a target only when a condition method returns `true` |
| [`[BeforeMap]` / `[AfterMap]`](#beforemap--aftermap) | Method | Calls a method before / after the mapping |
| [`[MapNested]`](#mapnested) | Method, multiple | Maps a member with a mapper method |
| [`[MapCollection]`](#mapcollection) | Method, multiple | Maps a collection member element by element with a mapper method; `Strategy`, `Converter` |
| [`[ValueConverter]`](#valueconverter) | Class, struct, method | Custom value converter class; `Method` |
| [`[CollectionConverter]`](#collectionconverter) | Class, struct, method | Custom collection converter class |

`[MapProperty<T>]` and `[MapConstant<T>]` are type-safe variants of `[MapProperty]` and `[MapConstant]`, which need C# 11 or later (generic attributes).

### Names in attributes

- **First argument convention** - For the attributes that map a destination member, the **first** argument is the **destination** (target) name. The **second** is the source for `[MapProperty]`, `[MapFrom]`, `[MapCollection]` and `[MapNested]`, and the method, constant or expression for `[MapUsing]`, `[MapCondition]`, `[MapConstant]` and `[MapExpression]`.
- The names of members are compared with the `NameComparison` of the method, and the names of methods are C# identifiers, matched exactly ([Name comparison](#name-comparison)).
- A target can also be the name of a parameter of the constructor a return-type mapper calls ([Values for constructor parameters](#values-for-constructor-parameters)).
- **Keywords as names** - A name that is a C# keyword, such as `@class`, keeps its `@` in the generated code, so members, enum members, parameters, methods, classes and namespaces named that way map like any other.

### `[Mapper]`

Marks a partial method, static or instance, as a mapper ([Mapper methods](#mapper-methods)).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoMap` | `bool` | `true` | Maps the members of the same name automatically; `false` maps only what the attributes name ([Disabling auto-mapping](#disabling-auto-mapping)) |
| `Strict` | `bool` | `false` | Warns of unmapped destination members (SMP0501), values that may be null going to targets that do not take null (SMP0502), and enum members without a member of the same name in the target enum (SMP0503) ([Strict mode](#strict-mode)) |
| `NameComparison` | `StringComparison` | `Ordinal` | Comparison of member names, applied to the automatic mapping and to the names written in the mapping attributes ([Name comparison](#name-comparison)) |
| `Culture` | `string?` | `null` | Culture name of the conversions, such as `"ja-JP"` ([Culture and formats](#culture-and-formats)) |
| `DateTimeFormat` | `string?` | `null` | Format of the conversions between the date and time types and `string` ([Formats](#formats)) |
| `NumberFormat` | `string?` | `null` | Format of the conversions between numbers and `string` ([Formats](#formats)) |

`Strict`, `NameComparison`, `Culture`, `DateTimeFormat` and `NumberFormat` take their defaults from the [profiles](#profiles): a value set on `[Mapper]` wins, also `Strict = false` under a strict profile, then the one of the class profile, then the one of the assembly profile, each setting on its own.

`Culture` on a method that has a `CultureInfo` parameter is not used, as the parameter gives the culture, and is reported as a warning (SMP0405).

### `[MapperProfile]`

Gives the defaults of the mapper methods of a class or a struct, or of the assembly ([Profiles](#profiles)).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Strict` | `bool` | `false` | Default of `Strict` of `[Mapper]` |
| `NameComparison` | `StringComparison` | `Ordinal` | Default of `NameComparison` of `[Mapper]` |
| `DefaultCulture` | `MapperCulture` | `Invariant` | Culture used when no culture name applies: `Invariant` or `Current` ([Default culture](#default-culture)) |
| `Culture` | `string?` | `null` | Default of `Culture` of `[Mapper]` |
| `DateTimeFormat` | `string?` | `null` | Default of `DateTimeFormat` of `[Mapper]` |
| `NumberFormat` | `string?` | `null` | Default of `NumberFormat` of `[Mapper]` |

### `[MapProperty]` / `[MapProperty<T>]`

`[MapProperty(target)]` and `[MapProperty(target, source)]` map the target from a source member.

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Target` | `string` | | Destination member, a dotted path into one (`Child.Value`), or a constructor parameter |
| `Source` | `string?` | the target name | Source member, or a dotted path (`Child.Name`) |
| `Converter` | `string?` | `null` | Method converting the source value ([Methods named by attributes](#methods-named-by-attributes)) |
| `NullValue` | `object?` (`T` for `[MapProperty<T>]`) | not set | Value the target gets for a null source ([`NullValue`](#null-substitution-nullvalue)) |
| `NullBehavior` | `NullBehavior` | `Default` | `Skip` leaves the target as it is for a null source ([`NullBehavior.Skip`](#keeping-the-destination-value-nullbehaviorskip)) |
| `Culture` | `string?` | `null` | Culture name of this mapping, over the `CultureInfo` parameter and the culture of the method ([Culture precedence](#culture-precedence)) |
| `DateTimeFormat` | `string?` | `null` | Date and time format of this mapping ([Formats](#formats)) |
| `NumberFormat` | `string?` | `null` | Number format of this mapping ([Formats](#formats)) |
| `Order` | `int` | `0` | Order among the assignments of the same kind ([Assignment order](#assignment-order)) |

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

- A dotted source reads through the members along it, under their null check, and a dotted target writes into the member it goes through ([Property paths](#property-paths)).
- A name that cannot be resolved is reported rather than silently dropped: SMP0213 for the source (not found, without a getter the mapper class can call, or obsolete as an error, also along its path), SMP0214 for the target.
- Two attributes mapping the same target, or a member as a whole and a member of it through a dotted path (`Child` and `Child.Value`), cannot both apply (SMP0101).
- The value converts to the target type as described in [Type conversion](#type-conversion), with the culture and the formats of [Culture and formats](#culture-and-formats).

### `[MapIgnore]`

`[MapIgnore(target)]` leaves the target, a member of the destination as a whole, out of the automatic mapping.

```csharp
[Mapper]
[MapIgnore(nameof(Destination.InternalId))]
[MapIgnore(nameof(Destination.TempValue))]
public static partial void Map(Source source, Destination destination);
```

- A dotted target (`Child.Value`) is reported (SMP0223), as the automatic mapping never assigns a member of a member on its own, so there is nothing to leave out.
- `[MapIgnore]` and an attribute mapping the same target contradict each other and are reported (SMP0101), whichever the attribute is. `[MapIgnore]` of a member with dotted paths into it is allowed: the member is left out of the automatic mapping, and the paths are applied.
- A target that is not found is reported (SMP0214): it has to be a property or a field of the destination, or a parameter of the constructor a return-type mapper calls.
- A member a parameter of the constructor of a return-type mapper assigns, or a parameter no member has, can be ignored as well: the parameter has no value then, so another constructor is chosen, an optional parameter is left out, or the destination is created without arguments ([Choosing the constructor](#choosing-the-constructor)). A destination that cannot be created otherwise is reported (SMP0216).
- A `required` member of the destination a return-type mapper creates cannot be ignored, as it has to be set when the destination is constructed (SMP0216), unless the constructor called has `[SetsRequiredMembers]`.
- In strict mode, `[MapIgnore]` of a member an optional constructor parameter left out would set leaves it to the default value without the warning ([Unmapped members](#unmapped-members)).

### `[MapUsing]`

`[MapUsing(target, method)]` assigns the value the method computes from the source to the target.

```csharp
[Mapper]
[MapUsing(nameof(Destination.FullName), nameof(CombineFullName))]
public static partial void Map(Source source, Destination destination);

private static string CombineFullName(Source source) => $"{source.FirstName} {source.LastName}";
```

- The method takes the source, followed by the [custom parameters](#custom-parameters) when it declares them. It is found and matched as described in [Methods named by attributes](#methods-named-by-attributes): it may be an instance method in an instance mapper, it is not generic, and it takes the source as its type or, by value, as a type it converts to implicitly (SMP0201 otherwise, also for an ambiguous call).
- It returns the type of the target, or a type C# converts to it implicitly, as the assignment does: an `int` for a `long` or an `int?` target, a class for a base class or an interface it implements. One only an explicit conversion takes, such as a `long` for an `int` target, is reported (SMP0202). A nullable reference it returns into a target not annotated as nullable is taken with `!`, as `[MapFrom]` takes it, unless `[return: NotNullIfNotNull]` of its first parameter says it returns one that is not null for the source, which it gets past the null check of the mapper.
- The target is a property or a field the mapper can assign, also through a dotted path such as `"Child.Value"`, which goes through its intermediate members as a `[MapProperty]` path does ([Unflatten](#unflatten)); the method is matched against the type of the target, a dotted one or a field as well. A target that is not found (a misspelled name, a method or a static member) or cannot be assigned (a `readonly` field) is reported (SMP0214).
- For a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the value goes to the constructor as the argument, and the method returns the type of the parameter then ([Values for constructor parameters](#values-for-constructor-parameters)).
- A dotted target writes into that member, which the automatic mapping then leaves out; it cannot go into a member the constructor assigns (SMP0222), nor through a nullable struct, whose `Value` is a copy no setter takes back (SMP0214).
- A `[MapUsing]` target is assigned by the method, not by a property mapping, so a `[MapCondition]` of it is reported (SMP0221).

### `[MapFrom]`

`[MapFrom(target, member)]` assigns the target from a member of the source: a parameterless method, or a property path.

```csharp
[Mapper]
[MapFrom(nameof(Destination.ItemCount), nameof(Source.GetItemCount))]  // instance method call
[MapFrom(nameof(Destination.NestedValue), "Nested.Value")]              // dot-notation path
public static partial void Map(Source source, Destination destination);
```

- The method is a parameterless instance method of the source the mapper class can call, not a generic one, found the way the call `source.Method()` binds: one of a base class, or of an interface the source interface extends, is found as well, the most derived one first, so that a method hiding another (`new`) wins. A `protected` or `private` one is not found.
- A property path goes through the properties, the inherited ones as well, with a getter the mapper class can call.
- A member that is not found is reported (SMP0204). A member obsolete as a warning is used, and one obsolete as an error is reported (SMP0204).
- The member gives the type of the target, or a type converting to it implicitly, as the method of `[MapUsing]` does (SMP0205 otherwise).
- A property path through members that may be null is read under their null check, as the source path of `[MapProperty]` is, a nullable struct through the struct it holds (`Location.Lat` as `source.Location.Value.Lat`): the target is left as it is when one of them is null, and a constructor argument or an object initializer entry gets `null` for a target that takes it, or `default`.
- A nullable reference going to a target not annotated as nullable is taken with `!`, as `[MapProperty]` takes it; so is a value with `[MaybeNull]` on the property, its getter, or the return of the method.
- A target that is not found on the destination is reported (SMP0203), as is a target the mapper cannot assign (SMP0214). For a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the value goes to the constructor as the argument, of the parameter's type.

```csharp
// [MapFrom(nameof(Destination.City), "Customer.Address.City")], Customer and Address nullable
if (source.Customer is not null && source.Customer.Address is not null)
{
    destination.City = source.Customer.Address.City;
}
```

### `[MapConstant]` / `[MapConstant<T>]`

`[MapConstant(target, value)]` and `[MapConstant<T>(target, value)]` assign a constant value to the target.

```csharp
[Mapper]
[MapConstant<int>("Version", 1)]
[MapConstant<string>("Status", "Active")]
[MapConstant<bool>("IsEnabled", true)]
public static partial void Map(Source source, Destination destination);
```

Non-generic variant: `[MapConstant("Status", "Active")]`. For a value computed at mapping time, use `[MapExpression]`, as in `[MapExpression("CreatedAt", "System.DateTime.Now")]`.

The constant is written as an expression of its own type: an enum as its member (as a cast for a value no member has, such as a combination of flags), a type as `typeof`, an array as a new array for each mapping, and a number, a character or a string as a C# literal spelled the same under any culture (`double.NaN` and the infinities by name, quotes and control characters escaped). A `byte`, `sbyte`, `short` or `ushort`, which has no literal of its own, is written as a cast, so that it keeps its type where the target takes any value (an `object` boxes it as a `short`, not an `int`). An enum member marked `[Obsolete]` is written as a member of the same value that is not obsolete when there is one, and as a cast of its value otherwise ([Obsolete members](#obsolete-members)).

```csharp
[MapConstant(nameof(Destination.Kind), Kind.Active)]                  // __d.Kind = global::Sample.Kind.Active;
[MapConstant(nameof(Destination.Access), Access.Read | Access.Write)] // __d.Access = (global::Sample.Access)3;
[MapConstant(nameof(Destination.ItemType), typeof(Item))]             // __d.ItemType = typeof(global::Sample.Item);
[MapConstant(nameof(Destination.Codes), new[] { 1, 2 })]              // __d.Codes = new int[] { 1, 2 };
[MapConstant(nameof(Destination.Ratio), 0.1)]                         // __d.Ratio = 0.1d;
[MapConstant(nameof(Destination.Flag), (short)-1)]                    // __d.Flag = (short)-1;
[MapConstant(nameof(Destination.Note), "tab\there")]                  // __d.Note = "tab\there";
```

- The value has to convert to the target type the way the compiler converts it (`1` to a `long` or a `byte`, `null` to a reference): `"abc"` for an `int`, `1.5` for a `float`, a `short` for a `byte`, an enum for a number or another enum, and `null` (or an array holding `null`) for a reference annotated as not null are reported (SMP0218), as is a value the generated code cannot refer to, such as a file-local type (SMP0220).
- The target is a property or a field the mapper can assign, also through a dotted path such as `"Child.Value"`, which goes through its intermediate members as a `[MapProperty]` path does ([Unflatten](#unflatten)). One that is not found (a misspelled name, a method or a static member) or cannot be assigned (a `readonly` field) is reported (SMP0214). A dotted target writes into that member, which the automatic mapping then leaves out; it cannot go into a member the constructor assigns (SMP0222), nor through a nullable struct, whose `Value` is a copy no setter takes back (SMP0214).
- For a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the value goes to the constructor as the argument, and is checked for the type of the parameter.

### `[MapExpression]`

`[MapExpression(target, expression)]` assigns the value of a C# expression to the target.

```csharp
[Mapper]
[MapExpression(nameof(Destination.CreatedAt), "System.DateTime.Now")]
[MapExpression(nameof(Destination.Total), "source.Price * source.Quantity")]
public static partial void Map(Source source, Destination destination);
```

- The expression is compiled as a local function that takes the mapper's parameters under the same names, so it can refer to them (e.g. `"source.Price * source.Quantity"`) and to the [custom parameters](#custom-parameters), and variables it declares with `out var` or patterns do not clash with those of other expressions. The function returns the type of the target, so the expression converts to the target as it would in a direct assignment.
- In a static mapper, the function is static. In an instance mapper, it is not, so in a class the expression can use the instance members of the mapper class (its fields, properties and methods) as well. In a struct it cannot, as C# does not let a local function in a struct use `this` (CS1673): compute such a value with a `[MapUsing]` instance method instead.
- The target follows the rules of the target of `[MapConstant]`: a property or a field the mapper can assign, also through a dotted path (SMP0214, SMP0222); for a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the value goes to the constructor as the argument, as a value of the parameter's type.
- An expression containing a reflection API (`Activator`, `Type.GetType`, `MethodInfo`, ...) is reported as a warning (SMP0403), as it may not be AOT-compatible ([NativeAOT and trimming](#nativeaot-and-trimming)).

### `[MapCondition]`

`[MapCondition(target, condition)]` assigns the target only when the condition method returns `true` for the source value ([Conditional mapping](#conditional-mapping)).

| Member | Type | Description |
|--------|------|-------------|
| `Target` | `string` | Destination member whose property mapping the condition guards, also a dotted path |
| `Condition` | `string` | Method taking the source value (and the custom parameters) and returning `bool` |

### `[BeforeMap]` / `[AfterMap]`

`[BeforeMap(method)]` and `[AfterMap(method)]` call the method before and after the mapping, with the source and the destination ([Before and after callbacks](#before-and-after-callbacks)). Each can be given once per mapper method.

| Member | Type | Description |
|--------|------|-------------|
| `Method` | `string` | Method taking the source and the destination, then the custom parameters when it declares them |

### `[MapNested]`

`[MapNested(target)]` and `[MapNested(target, source)]` map the target with a mapper method ([Nested objects](#nested-objects)).

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Target` | `string` | | Destination member, or a constructor parameter |
| `Source` | `string?` | the target name | Source property (not a dotted path) |
| `Mapper` | `string?` | `null` | Mapper method; required (SMP0211 without it) |
| `Order` | `int` | `0` | Order among the `[MapNested]` assignments ([Assignment order](#assignment-order)) |

### `[MapCollection]`

`[MapCollection(target)]` and `[MapCollection(target, source)]` map a collection member element by element with a mapper method ([Collections](#collections)).

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `Target` | `string` | | Destination member, or a constructor parameter |
| `Source` | `string?` | the target name | Source property (not a dotted path) |
| `Mapper` | `string?` | `null` | Element mapper method; required (SMP0210 without it) |
| `Converter` | `string?` | `null` | Method of the collection converter to call, instead of the one chosen by the target type ([Collection converters](#collection-converters)) |
| `Strategy` | `CollectionStrategy` | `Replace` | `Replace` assigns a new collection; `InPlace` clears the existing one and refills it ([Refilling the existing collection](#refilling-the-existing-collection)) |
| `Order` | `int` | `0` | Order among the `[MapCollection]` assignments ([Assignment order](#assignment-order)) |

### `[ValueConverter]`

`[ValueConverter(typeof(...))]` on a mapper method, or on a class or a struct for all its mapper methods, names the class the value conversions call ([Custom value converters](#custom-value-converters)).

| Member | Type | Default | Description |
|--------|------|---------|-------------|
| `ConverterType` | `Type` | | Converter class, which may be nested or generic |
| `Method` | `string` | `"Convert"` | Name the methods are looked up by: `{Method}To{TargetType}` for the specialized methods, `{Method}<TSource, TDestination>` for the generic fallback |

### `[CollectionConverter]`

`[CollectionConverter(typeof(...))]` on a mapper method, or on a class or a struct for all its mapper methods, names the class whose methods build the target collections of `[MapCollection]` ([Collection converters](#collection-converters)).

| Member | Type | Description |
|--------|------|-------------|
| `ConverterType` | `Type` | Collection converter class |

### Enums

| Enum | Members | Description |
|------|---------|-------------|
| `NullBehavior` | `Default` (0), `Skip` (1) | What a null source does to the target of a `[MapProperty]`: `Default` assigns the value for it (`NullValue`, `null` or `default`), `Skip` leaves the target as it is |
| `CollectionStrategy` | `Replace` (0), `InPlace` (1) | How `[MapCollection]` sets the target: `Replace` assigns a new collection (default), `InPlace` clears the existing instance and adds the mapped elements, keeping a reference held elsewhere (e.g. by data binding) |
| `MapperCulture` | `Invariant` (0), `Current` (1) | The culture the conversions use when no culture name applies ([Default culture](#default-culture)) |

---

## Auto-mapping and name matching

### Auto-mapping

Same-name, compatible-type properties are mapped automatically.

```csharp
[Mapper]
public static partial void Map(Source source, Destination destination);
```

A property the mapper cannot assign (get-only, or a setter it cannot call such as `private set`) is left out, unless the constructor that construction calls takes it ([Constructors and records](#constructors-and-records)). Named in a mapping attribute, such a property is reported (SMP0214). A void mapper never constructs, so a member only a constructor assigns is left out, like any get-only property.

The properties a type inherits are included: those of its base classes, and for an interface, those of the interfaces it extends. A name is taken as `x.Name` binds to it in the generated code: to the most derived member of the name the mapper class can access, whatever it is. A property overriding one of a base type, or hiding it with `new`, stands for it, and one overriding the getter only is assigned through the setter it inherits. A name whose member is not a public instance property (an `internal` property, a field, a method) is not mapped automatically, nor is the property it hides; a `private` member hides nothing from the mapper class. A name an interface inherits from two interfaces, neither hiding the other, is ambiguous and not mapped either. Indexers are left out. The same holds for the names written in the mapping attributes. A source property is read through its getter, so one without a getter the mapper class can call (a `private get`, or set-only) is not a source.

A property marked `[Obsolete]` is left out ([Obsolete members](#obsolete-members)), and a member a dotted target path writes into is mapped through the path instead ([Dotted paths and the automatic mapping](#dotted-paths-and-the-automatic-mapping)).

### Disabling auto-mapping

```csharp
[Mapper(AutoMap = false)]
[MapProperty(nameof(Destination.Id), nameof(Source.Id))]
public static partial void Map(Source source, Destination destination);
// Only 'Id' is mapped; other properties are ignored.
```

### Name comparison

`NameComparison` applies to the names written in mapping attributes, not just to auto-mapping. The comparison is the one of `[Mapper]`, or else the one of the `[MapperProfile]` of the class, or else the one of the `[MapperProfile]` of the assembly, or else `Ordinal`. An exact match always wins; the configured comparison is only a fallback, so the default (`Ordinal`) matches exactly.

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

This holds for every name of a member written in a mapping attribute: the targets, properties and fields alike and each segment of a dotted path, including target-only attributes such as `[MapIgnore]`, the sources, and the member of `[MapFrom]`. When several members match ignoring case, the first one declared wins, a property before a field. The names of methods (`Converter`, `[MapCondition]`, `[MapUsing]`, `[BeforeMap]` / `[AfterMap]`, and the mappers of `[MapCollection]` / `[MapNested]`) are C# identifiers and are matched exactly. A differently cased name that does not match under the comparison is reported as not found (SMP0213, SMP0214).

### Assignment order

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

### Obsolete members

The automatic mapping leaves out a property marked `[Obsolete]`, obsolete as a warning or as an error, as a source and as a destination: one marked itself, on the accessor it is read or assigned through, or on the property it overrides (C# reports the one the override overrides). Strict mode does not report it, while a `required` one still has to be mapped (SMP0303).

A member an attribute names is used when obsolete as a warning, which C# reports (CS0618), and reported with the diagnostic of the attribute when obsolete as an error, as the generated code could not use it (CS0619): a property or a field, a segment of a dotted path, and the method of `[MapFrom]` (SMP0213, SMP0204 or SMP0206 for a source, SMP0214 for a target). A method these attributes name that is obsolete as an error does not match either: a `Converter` (SMP0104), a `[MapCondition]` method (SMP0106), a `[MapUsing]` method (SMP0201), a `[BeforeMap]` / `[AfterMap]` callback (SMP0102 / SMP0103), and the mapper method of `[MapNested]` / `[MapCollection]` (SMP0211 / SMP0210).

A constructor obsolete as an error is never called, and one obsolete as a warning only when nothing else will do ([Choosing the constructor](#choosing-the-constructor)). An intermediate member of a dotted target is created with `new T()` when the constructor that call binds to can be called: one obsolete as an error makes the type one that cannot be created, and the path writes into the member the destination holds, while one obsolete as a warning is called, warning, as nothing else creates the member.

The conversions the generated code makes on its own do not call a member obsolete as an error either: a conversion operator (`implicit` / `explicit`), a method of the `[ValueConverter]` / `[CollectionConverter]` class, the `ToString(format, provider)` and `Parse` of the conversions, and the constructor a collection class is created with. Another conversion takes over when there is one (the generic method of the converter class for a specialized one, the `Parse` taking a `string` for the one taking a span), and otherwise the value is reported as having no conversion (SMP0402), the converter method as not matching (SMP0104), or the collection as one the generated code cannot create (SMP0217). One obsolete as a warning is called, warning (CS0618).

An enum member marked `[Obsolete]`, as a warning or as an error, is written as a cast of its value, such as `(Color)2`, so that the generated code neither warns nor fails: in the switch converting an enum to another enum (the members still match by name) or to or from a string, and in an enum constant of `[MapConstant]` or `NullValue`, which is written as a member of the same value that is not obsolete when there is one.

---

## Methods named by attributes

The methods named by `[MapUsing]`, `[MapCondition]` and `[BeforeMap]` / `[AfterMap]`, the `Converter` of `[MapProperty]`, and the `Mapper` of `[MapNested]` / `[MapCollection]` are called by the generated code by their simple name and without type arguments. Their names are C# identifiers, matched exactly. A generic method is not taken (it is reported as a method that does not match), nor is one obsolete as an error ([Obsolete members](#obsolete-members)), nor one whose parameter modifier cannot take the argument the generated code passes (such as `ref` for a property value; [Diagnostics.md](../Diagnostics.md) lists the arguments).

### Lookup

The methods are looked up as C# looks up the name of a call: in the mapper class and its base classes (a `protected` method included, as the mapper class can call it), or, when none of them has a member of the name the call can invoke, in the class containing the mapper class and its base classes, and so on outward, and last in the types the `global using static` directives import, which the generated file sees as well (a `using static` directive of one file only is not seen, and a type imported gives the methods it declares, not those it inherits nor its extension methods). As in C#, the first class having a member of the name the call can invoke, which the mapper class can access, is the only one looked in, even when its methods do not take the arguments; a member the call cannot invoke, such as a property or a field that is not a delegate, or a nested type, is passed over. A method of a derived class hides one of a base class of the same signature.

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

Methods shared by several mapper classes can live in a class imported with `global using static` (`global using static MyApp.Converters;` in one file of the project), or in a base class the mapper classes derive from. A conversion applied by the types of the source and the target, rather than named for each property, goes to a `[ValueConverter]` class ([Custom value converters](#custom-value-converters)).

### Instance and static methods

A static mapper calls static methods only. An instance mapper calls the methods of the mapper class and its base classes on itself, so they may be instance methods as well as static ones, and can use the fields of the instance, such as services injected through the constructor. The methods of a class containing the mapper class and of the types imported with `global using static` have to be static, as there is no instance of them to call them on; an instance method there is not taken, and naming it is reported as a method that does not match. An instance mapper may be declared in a struct as well.

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

    private decimal CalculateTax(Invoice source) => taxService.Calculate(source.Amount);  // instance method

    private static string Normalize(string value) => value.Trim();                       // static method
}
```

A static mapper naming a method that the mapper class and its base classes have only as instance methods cannot call it, and is reported at the attribute naming it (SMP0107): make the mapper an instance method, or the method static. An extension method mapper is static, so it calls static methods only.

A `[MapExpression]` of an instance mapper in a class can use the instance members as well; in a struct it cannot, as a local function there cannot use `this` (CS1673), so a `[MapUsing]` instance method takes its place ([`[MapExpression]`](#mapexpression)).

### Taking the value

The method of `[MapUsing]`, the `Converter` of `[MapProperty]` and the method of `[MapCondition]` take the value (the source for `[MapUsing]`, the source member for the others) as its own type, or, to a parameter taken by value, as a type it converts to implicitly, as C# passes it: a base class or an interface (`private static string Label(IHasName x)` for a `Person` source, a converter taking `IEnumerable<string>` for a `List<string>` member), `object` or an interface a struct implements (boxing, `IFormattable` for a `DateTime`), a wider number (`long` for an `int`), a nullable struct (`int?` for an `int`), and a user-defined implicit conversion, not one obsolete as an error. A parameter taken by `in` takes the type of the value only.

A nullable struct goes to a converter or a condition taking the struct it holds (an `int` for an `int?`), or, by value, a type the value it holds converts to implicitly (a `long` or a `double`, or a user-defined conversion), as its `Value`, for a value only, as to a parameter that does not take null ([Null substitution](#null-substitution-nullvalue), [Conditional mapping](#conditional-mapping)): a method taking the struct itself goes first, then one taking the nullable struct by a conversion (`long?`, `object`), then one taking the value it holds by a conversion. A value going to a parameter through a user-defined conversion goes for a value only as well when the parameter of the conversion operator does not take null.

A method only an explicit conversion takes the value to, such as a base class to a derived class, does not match, and is reported (SMP0104, SMP0106, SMP0201).

The [custom parameters](#custom-parameters) the method declares follow the value. The callbacks of `[BeforeMap]` / `[AfterMap]` take the source and the destination ([Before and after callbacks](#before-and-after-callbacks)), and the mappers of `[MapNested]` / `[MapCollection]` the source member or element ([Nested objects](#nested-objects), [Element mappers](#element-mappers)), each followed by the custom parameters it declares as well.

### Overloads

Of the overloads, the call binds to the one C# binds it to: one taking the type of the value first, and otherwise the one whose parameter is the most specific (a class over its base class or interface, `long` over `object` for an `int`). The call has to bind to the method chosen: one taking the type of the value it does not bind to gives way to the one it binds to (a method of a derived class taking the value leaves out those of its base classes, and one taking by value goes before one taking by `in`). A call that is ambiguous (CS0121), or that binds, or may bind, to a method not matched (one taking the value by an explicit conversion, a generic one, one with optional parameters or `params`, or one obsolete as an error) is reported as a method that does not match, and one binding to a method that returns another type by its return type (SMP0105, SMP0202, and SMP0106 for a condition not returning `bool`).

### Return values

- The method of `[MapUsing]` returns the type of the target, or a type C# converts to it implicitly, as the assignment does (SMP0202 otherwise). A nullable reference it returns into a target not annotated as nullable is taken with `!`, unless `[return: NotNullIfNotNull]` of its first parameter says it returns one that is not null for the source, which it gets past the null check of the mapper.
- The `Converter` of `[MapProperty]` returns the target type, or a type converting to it implicitly as the method of `[MapUsing]` does (SMP0105 otherwise). A nullable reference it returns into a target not annotated as nullable is taken with `!`, unless it is given a value that is not null and `[return: NotNullIfNotNull]` of its first parameter says it returns one for it.
- The method of `[MapCondition]` returns `bool` (SMP0106 otherwise).
- For a constructor argument, the target type is the type of the parameter ([Conversion of constructor arguments](#conversion-of-constructor-arguments)).

---

## Property paths

A target or a source of `[MapProperty]` can be a dotted path, to flatten or unflatten nested members. The targets of `[MapConstant]`, `[MapExpression]` and `[MapUsing]` go through their intermediate members the same way, and the property path of `[MapFrom]` reads as a source path does. The source of `[MapNested]` and `[MapCollection]` is a property of the source type, not a dotted path (SMP0206).

### Flatten

Nested source to flat destination:

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

A nullable struct along the path, such as `GeoPoint? Location` for `Location.Lat`, is read through the struct it holds under the same check (`source.Location.Value.Lat`). C# does not follow the null check of a value read through five members or more (the `Value` of a nullable struct counting as one), so where the generated code checks such a value before a converter, a condition or a conversion uses it, it takes the value into a variable (`if (source.Location.Value.In.Value.V is { } __value_V)`) and uses that. The value at the end of a path converts as a member does: an enum by member name to another enum and to and from text, and to and from a number, with the culture and the formats of the method.

The path goes through the properties, the inherited ones as well, with a getter the mapper class can call; one without, or obsolete as an error, is reported (SMP0213). A member along it with `[MaybeNull]` may be null as well ([`[MaybeNull]` members](#maybenull-members)).

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

A constructor argument or an object initializer entry, which cannot be left as it is, gets `NullValue`, or `null` for a target that takes it, or `default` ([Conversion of constructor arguments](#conversion-of-constructor-arguments)).

### Unflatten

Flat source to nested destination:

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

A dotted target cannot go through a nullable struct, as `Location.Lat` for a destination member `GeoPoint? Location`: the struct it holds is read through `Value` as a copy, which no setter takes back. Such a path is reported (SMP0214) with a message saying so; map the member as a whole instead, with `[MapUsing]` for example. A dotted source reads through one ([Flatten](#flatten)). A path through an intermediate member whose getter the mapper cannot call is reported (SMP0214) as well.

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

The intermediate members created keep the nullable annotations of their type arguments ([Nullable annotations of created instances](#nullable-annotations-of-created-instances)).

### Dotted paths and the automatic mapping

A dotted path into a member, of `[MapProperty]`, `[MapConstant]`, `[MapExpression]` or `[MapUsing]` alike, takes the place of the automatic mapping of that member, which is then not mapped as a whole: the path writes into the member the destination holds or creates, never into the object of the source. A member left out with `[MapIgnore]` still takes the dotted paths into it. A member mapped as a whole by an attribute cannot also take a dotted path (SMP0101), nor can a member the constructor of a return-type mapper assigns from an argument, such as a parameter of a positional `record`, as the path would write into the object passed to the constructor (SMP0222). A `required` member a return-type mapper sets is created in the object initializer when its type can be created, and the paths write into it after construction, an `init`-only member at the end in the initializer.

---

## Nested objects

`[MapNested]` maps a member with a mapper method, typically another `[Mapper]` method.

```csharp
[Mapper]
[MapNested(nameof(Destination.Child), nameof(Source.Child), Mapper = nameof(MapChild))]
public static partial void Map(Source source, Destination destination);
```

Generated code:

```csharp
destination.Child = source.Child is not null ? MapChild(source.Child!) : default!;
```

- The mapper has to be given with `Mapper` (SMP0211 without it, the message saying so). It is found as the other [methods named by attributes](#methods-named-by-attributes) are, so in an instance mapper it may be an instance method, and it takes the [custom parameters](#custom-parameters) it declares after the source (and the instance of a void one); a `CultureInfo` parameter so gives its culture to the nested mapping as well.
- The source is a property of the source type, not a dotted path; it defaults to the target name. A source that is not found, has no getter the mapper can call, or is obsolete as an error is reported (SMP0206), and so is a target that is not found (SMP0207). A target without a setter or an `init` accessor the mapper can call, or an `init`-only one in a void mapper, is reported (SMP0212).
- A null source member goes to a mapper whose parameter takes null as well, as in `DestinationChild MapChild(SourceChild? source)`, which decides what the target gets for it (`destination.Child = MapChild(source.Child);`), and a void one fills the instance created for the target; a mapper whose parameter does not take null is called for a value only, the target getting `default` for a null source, as above. A source member declared with nullable annotations disabled, or with `[MaybeNull]`, may be null as well ([Null handling](#null-handling)).
- The result of a mapper returning a nullable reference goes with `!` to a target not annotated as nullable, unless the mapper gets a value that is not null and `[return: NotNullIfNotNull]` of its first parameter says it returns one for it, as a generated mapper declares: `destination.Child = MapChild(source.Child);` for a source that is not nullable and `DestinationChild? MapChild(SourceChild? source)`.
- A void mapper fills an instance the generated code creates with `new()`, with the nullable annotations of the type arguments of the target (`new Box<string?>()`), so it matches only when the target type allows that: a struct, or a class that is not abstract with a constructor callable without arguments and no required members that constructor leaves unset.
- The mapper takes the source member as its type, or as one the member converts to by an implicit reference conversion (a base class or an interface, taken by value), and returns the target's type, one converting to it the same way (such as a class for a target of an interface it implements), or the struct of a nullable struct target. A nullable struct member goes to a mapper taking the struct as the value it holds, after the null check, and a null one gives `default`, as a null reference does: `source.Point is not null ? MapPoint(source.Point.Value) : default!`. A void mapper takes the instance created for the target as its type, or, by value, as one it converts to. A mapper the source or the target does not convert to this way does not match (SMP0211).
- Of the overloads, the one the call binds to is used, as C# binds it: one of the types themselves over one through conversions. A call binding to a method that does not match (a more specific one returning another type, a generic one, one with optional parameters or `params`, or one obsolete as an error), or two overloads matching through conversions alike, which would make the call ambiguous, are reported (SMP0211).
- For a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the value of the parameter's type is made before construction and passed as the argument. For an `init`-only target, or a `required` one the constructor called does not set, a return-type mapper makes the value before construction and sets it in the object initializer, as `[MapCollection]` does ([`init`-only and `required` members](#init-only-and-required-members)).
- A `[MapNested]` target is assigned by the mapper, so a `[MapCondition]` of it is reported (SMP0221).

---

## Collections

### Mapping the elements

`[MapCollection]` maps a collection member element by element with an element mapper method, which has to be given with `Mapper` (SMP0210 without it, the message saying so). The source is a property of the source type, not a dotted path (SMP0206), and defaults to the target name.

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

The loop is generated inline, shaped by the source and target collection types. The source is a type implementing `IEnumerable<T>`, `Memory<T>` or `ReadOnlyMemory<T>` (SMP0208 otherwise), and the target a type implementing `IEnumerable<T>` (SMP0209 otherwise). A source or a target that is not found is reported (SMP0206, SMP0207), and so is a target without a setter or an `init` accessor the mapper can call, or an `init`-only one in a void mapper (SMP0212; `InPlace` refills the instance it holds instead).

A null source collection sets the target to `default`. A source collection declared nullable, with nullable annotations disabled, or with `[MaybeNull]`, is checked for null the same way.

For a member the constructor of a return-type mapper assigns, or a parameter the target names when no member has its name, the collection of the parameter's type is made before construction and passed as the argument. For an `init`-only member, or a `required` one the constructor called does not set, it is made before construction and set in the object initializer ([`init`-only and `required` members](#init-only-and-required-members)). A `[MapCollection]` target is assigned by the loop, so a `[MapCondition]` of it is reported (SMP0221).

### Target collections

The target gets the collection the loop builds:

- a `List<T>` for `List<T>` and its interfaces;
- an array for an array;
- a `HashSet<T>` for sets;
- a `Dictionary<TKey, TValue>` for `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>`, filled with the `KeyValuePair<TKey, TValue>` pairs the element mapper returns;
- the immutable or frozen collection of its type: `ImmutableArray<T>`, `ImmutableList<T>`, `ImmutableHashSet<T>` and their interfaces, and `FrozenSet<T>`. The other immutable and frozen collections, such as `ImmutableDictionary<TKey, TValue>` or `FrozenDictionary<TKey, TValue>`, are reported (SMP0217) unless a collection converter builds them;
- a collection class the mapper can create, such as `ObservableCollection<T>` or `class ItemList : List<Item>`, built with its own constructor and filled through `ICollection<T>`. One it cannot create is reported (SMP0217) unless a collection converter builds it.

A target that cannot take the collection built for it is reported (SMP0217). The collection created keeps the nullable annotations of the elements of the target (`List<DestinationChild?>`).

### Element mappers

- The element mapper is found as the other [methods named by attributes](#methods-named-by-attributes) are, so in an instance mapper it may be an instance method, and it takes the [custom parameters](#custom-parameters) it declares after the element (and the instance of a void one), except when it is handed to a collection converter as a delegate, which cannot pass them (SMP0210). It matches the element types the way the mapper of `[MapNested]` does ([Nested objects](#nested-objects)), and a call binding to a method that does not match is reported (SMP0210).
- A void element mapper `(SourceChild, DestinationChild)` fills a `new DestinationChild()`, so the element type has to be creatable with `new()` (SMP0210 otherwise).
- The result of a mapper declared to take null, as in `DestinationChild? MapChild(SourceChild? source)`, which returns null only for a null source, is taken with `!` for elements not annotated as nullable, as it is for the target of `[MapNested]`; an element that is not null, for which `[return: NotNullIfNotNull]` of the first parameter says the result is not null, as a generated mapper declares, gives a result taken as it is (`__dst[__i] = MapChild(__src[__i]);` for a `List<SourceChild>` source).
- A nullable struct element goes to a mapper taking the struct as the value it holds, and a null one gives `default`.
- A reference element that may be null, a nullable one or one declared with nullable annotations disabled, goes to a mapper whose parameter does not take null the same way, when it has a value, and a null one gives `default` (`__src[__i] is { } __value ? MapChild(__value) : default!`).
- A collection converter, which takes the mapper as a delegate, gets every element; the parameters of the mapper then have to match those of the delegate (by value for `Func` / `Action`).

### Collection classes

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

### Collections as a whole

A mapper maps an object, not a collection: one whose source or destination is a collection of the framework (a list, a set, a dictionary or one of their interfaces, `PriorityQueue<TElement, TPriority>`, and the immutable, frozen, concurrent and object model ones), a class deriving from one (`class ItemList : List<Item>`), an array or a tuple, or a type parameter constrained to one (`T Create<T>(Item source) where T : List<ItemDto>, new()`), is reported (SMP0007), as it would map the members of the collection (`Count`, `Capacity`) and none of its elements. Map the elements with a mapper of the element type, or map a type holding the collection with `[MapCollection]`:

```csharp
[Mapper]
public static partial ItemDto ToDto(Item source);

// Not [Mapper] List<ItemDto> ToDtos(List<Item> source), which is reported
var dtos = items.Select(ToDto).ToList();
```

A type of its own that only implements `IEnumerable<T>`, such as a page of items with its total count, is mapped by its members as any other type.

### Collection converters

With a collection converter (`[CollectionConverter]` on the mapper method or its class), its method is called instead of the loop, as in `CustomCollectionConverter.ToList<SourceChild, DestinationChild>(source.Children, MapChild)!`.

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

The method is picked by the target type (`ToList`, `ToArray`, `ToHashSet`, `ToImmutableArray`, ...) unless `Converter` of `[MapCollection]` names one, and is called as `Method<TSourceElement, TTargetElement>(source, mapper)`. `Converter` names a method of the `[CollectionConverter]` type or, without one, of `DefaultCollectionConverter`. A method that is missing, does not take the source collection, does not meet the constraints of its type parameters, or returns something the target property cannot take is reported (SMP0104). A method of the converter class obsolete as an error is not called, and is reported as not matching (SMP0104); one obsolete as a warning is called.

`DefaultCollectionConverter` provides these methods for both a function mapper (`Func<TSource, TDest>`) and a void action mapper (`Action<TSource, TDest>`, which fills a `new TDest()`):

| Method | Result | Source overloads (`Func` mapper) |
|--------|--------|----------------------------------|
| `ToArray` | `TDest[]?` | `IEnumerable<T>`, `T[]`, `List<T>`, `ReadOnlySpan<T>`, `IReadOnlyCollection<T>` |
| `ToList` | `List<TDest>?` | same |
| `ToHashSet` | `HashSet<TDest>?` | same |
| `ToImmutableArray` | `ImmutableArray<TDest>` | same |
| `ToImmutableList` | `ImmutableList<TDest>?` | same |
| `ToImmutableHashSet` | `ImmutableHashSet<TDest>?` | same |
| `ToFrozenSet` | `FrozenSet<TDest>?` | same |

A null source gives `null`, or an empty `ImmutableArray<TDest>`. The `Action` overloads take an `IEnumerable<T>` source, need the `new()` constraint, and are marked `RequiresUnreferencedCode` and `RequiresDynamicCode` ([NativeAOT and trimming](#nativeaot-and-trimming)).

`CollectionStrategy.InPlace` always emits the loop, so a collection converter is not used with it.

### Refilling the existing collection

By default (`CollectionStrategy.Replace`) the target gets a new collection. `CollectionStrategy.InPlace` keeps the target instance, clears it and adds the mapped elements, which preserves a reference held elsewhere, as in data-binding scenarios:

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

An instance that is read-only at run time behind a type such as `IList<T>` (an array, for example) throws `NotSupportedException` from `Clear`; the target keeps its instance, as `InPlace` promises, rather than being replaced behind the caller's back. `InPlace` always emits the loop; a collection converter is not used. A null source leaves the target as it is, neither cleared nor replaced.

A member the constructor of a return-type mapper assigns from an argument, and a `required` member of the destination a return-type mapper creates, have to be set before construction, where there is no instance to refill, and are reported (SMP0219), a `required` one unless the constructor called has `[SetsRequiredMembers]`; an `init`-only member is refilled in the instance it holds, as a get-only one is.

---

## Constructors and records

### Constructor calls

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

The constructor a return-type mapper calls, and whether it passes arguments, is chosen as described in [Choosing the constructor](#choosing-the-constructor).

### Void mappers and constructors

A void mapper never constructs, so the constructors of the destination do not affect it: a member only a constructor assigns is left out of the automatic mapping, like any get-only property.

> A `void` mapper cannot assign `init`-only members, such as the properties of a positional `record` (also at the end of a dotted path, or through an `init`-only struct property), nor a member only a constructor assigns named by `[MapProperty]` (SMP0302).

### Values for constructor parameters

A get-only property assigned through a constructor can be remapped:

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

A member the constructor assigns can take the value of `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` or `[MapCollection]`, which goes to the argument (`new Dst(Build(src))`). `[MapNested]` and `[MapCollection]` make it before construction; `InPlace` has no instance to refill there (SMP0219).

A dotted target into a member the constructor assigns, such as `[MapProperty("Child.Value", ...)]` for `record Dst(Child Child)`, is reported (SMP0222): it would write into the object passed to the constructor, which is the source's own when the argument copies it.

### `init`-only and `required` members

`[MapNested]` and `[MapCollection]` make the value of an `init`-only member, and of a `required` one the constructor called does not set, before construction, and the object initializer sets it, with the null handling, the element annotations and the mappers as for any other target; a void mapper cannot assign an `init`-only member (SMP0212). The automatic mapping and the other attributes assign `init`-only members in the object initializer of a return-type mapper as well.

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

The `required` members of the destination, properties and fields of any accessibility and those of its base classes, are set in the object initializer of a return-type mapper, so each has to be mapped (SMP0303) and cannot be ignored (SMP0216). The automatic mapping and `[MapProperty]` go through the public properties, while `[MapConstant]`, `[MapExpression]` and `[MapUsing]` also take an `internal` member. A required member the dotted paths write into is created in the object initializer when its type can be created, and reported (SMP0303) only when it cannot be (abstract, without a constructor callable without arguments, or with required members of its own). A required member a parameter of the constructor called assigns is reported (SMP0304): the object initializer would have to set it again, replacing what the constructor made of the argument, so the constructor needs `[SetsRequiredMembers]`. When the constructor called has `[SetsRequiredMembers]`, they are not required: an unmapped one keeps what the constructor set, the mapped ones are still set in the object initializer, and `[MapNested]` / `[MapCollection]` can assign one after construction. A void mapper fills an instance that already exists, so they do not concern it.

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

Constructor arguments go through the same conversion pipeline as ordinary property assignments, so type conversion, `Converter`, `NullValue` and the culture and format settings all apply. The same holds for `init`-only members assigned in the object initializer. An argument is checked and converted for the type of the parameter, which need not be the type of the member it assigns: a `string` parameter for an `int` property takes the conversion to `string`, as a `string` property would. The values of `Converter`, `NullValue` and the attributes are checked by the rules of a property of the parameter's type as well: a converter or a method returning the member's type is reported for a parameter of another type (SMP0105, SMP0202, SMP0205, SMP0211, SMP0218), as it would be for such a property.

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

Statement-only options cannot apply to a constructor argument or an object initializer entry: `[MapCondition]` has no way to leave the member unassigned, and `NullBehavior.Skip` has no previous value to keep. Both are rejected with SMP0215.

---

## Null handling

### Nullable values

| Source type | Destination type | Behavior |
|-------------|-----------------|----------|
| `T?` | `T?` | Copied as-is (including null) |
| `T?` | `T` (leaf) | `default!` assigned when null |
| `T` | `T?` | Copied as-is |
| `T` | `T` | Copied as-is |

Nullable intermediate paths on the **source side**, of `[MapProperty]` and `[MapFrom]` alike, are guarded with `if (... is not null)`, a nullable struct read through the struct it holds (`source.Location.Value.Lat`); when one is null, a mapping with `NullValue` takes it, and the others leave their targets as they are ([Flatten](#flatten)).
Nullable intermediate paths on the **destination side** are auto-instantiated with `??= new` when the mapper can assign and create them; otherwise the instance they hold is filled. A dotted target cannot go through a nullable struct (SMP0214) ([Unflatten](#unflatten)).

### Null substitution (`NullValue`)

```csharp
[Mapper]
[MapProperty(nameof(Destination.Name),  nameof(Source.Name),  NullValue = "Unknown")]
[MapProperty(nameof(Destination.Count), nameof(Source.Count), NullValue = 0)]
public static partial void Map(Source source, Destination destination);
```

The value is written like a `[MapConstant]` value and has to convert to the target type, as in `source.Count ?? 0` (SMP0218 otherwise, and SMP0220 for a value the generated code cannot refer to); `NullValue = null` needs a target that takes null. An enum member marked `[Obsolete]` is written as a member of the same value that is not obsolete when there is one ([Obsolete members](#obsolete-members)).

With a `Converter`, which takes the source as it is, a null source takes `NullValue`, and the converter is called for a value only, as in `source.Count is not null ? ToText(source.Count) : "none"`. A dotted source whose intermediate member is null takes `NullValue` as well ([Flatten](#flatten)).

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

A `Converter`, which takes the source as it is, is then called only for a value as well. A dotted source whose intermediate member is null leaves the target as it is too.

A member assigned through a constructor or an object initializer has no previous value to keep, so `NullBehavior.Skip` is rejected there (SMP0215).

### Nullable annotations disabled

A reference type declared with nullable annotations disabled (`#nullable disable`, or a library built without them) says nothing about null, so a value of it may be null: the source parameter, the source members and the elements of the source collections of such a type are handled as nullable ones. They are checked before they are read through or passed to a converter, a condition, the mapper of `[MapNested]` or an element mapper whose parameter does not take null, and `NullValue` and `NullBehavior.Skip` apply to them. Strict mode does not report them as values that may be null (SMP0502).

### `[MaybeNull]` members

A source member of a reference type with `[MaybeNull]`, on the property or on the return of its getter (`[MaybeNull] public string Name { get; set; }`), may be null as C# reads it, so it is handled as one of a nullable type: it is checked the same way, `NullValue`, `NullBehavior.Skip` and `[MapCondition]` apply to it, and strict mode reports it (SMP0502). The attributes of the property a path binds to count, so an override without the attribute is read as not null, and a method of `[MapFrom]` with `[return: MaybeNull]` gives a value that may be null as well.

### Nullable parameters and return types

A source parameter (or the destination parameter of a void mapper) declared nullable, as in `Map(Src? source)`, is checked before anything is mapped, and so are a source parameter and the destination parameter of a void mapper declared with nullable annotations disabled; the custom parameters are passed on as they are. When one is null nothing is mapped: a return-type mapper returns `default`, and a void mapper returns without touching the destination.

A return-type mapper whose source may be null returning a nullable type, as in `Dst? Map(Src? source)` or `Point? Map(Src? source)`, returns null for a null source only, so its implementation declares `[return: NotNullIfNotNull("source")]`, by the name of the parameter, and a caller passing a source that is not null uses the result without a nullable warning. The declaration may have the attribute as well. It is left out when the compilation has no `NotNullIfNotNullAttribute` the mapper class can use (.NET Standard 2.0 or .NET Framework without a copy of it). A return-type mapper whose source is declared nullable returning a type that does not take null, as in `Dst Map(Src? source)`, returns `default` for a null source, which strict mode reports (SMP0502).

A return type declared as a nullable struct, as in `Point? Map(Src source)`, is created and filled as the struct it holds. A source parameter declared as one, as in `Map(Point? source)`, is reported (SMP0006), as it has none of the members of the struct it holds: take the struct, and check null before the call.

### Nullable annotations of created instances

The instances the generated code creates keep the nullable annotations of their type arguments, as the declarations say: a destination declared as `Box<string?>` is created as `new Box<string?>()`, and so are the intermediate members of a dotted target, the `required` members created in the object initializer, the collections `[MapCollection]` builds, and the instances a void mapper of `[MapNested]` / `[MapCollection]` fills.

---

## Type conversion

### Direct assignments

Same-type and implicitly convertible assignments are generated without a converter: a numeric widening, a value to its nullable type, and an implicit reference conversion, through variance as well (`IReadOnlyList<Circle>` to `IReadOnlyList<Shape>`, `Circle[]` to `Shape[]`). The same enum on both sides is copied as it is, which keeps a value no member has, such as a combination of flags.

### Specialized methods

When a conversion is needed, the specialized method of the value converter for the types is called, named `ConvertTo{TargetType}` (`{Method}To{TargetType}` for a [custom value converter](#custom-value-converters)). The direct calls are friendly to JIT inlining, and no reflection is used at run time.

```csharp
// string -> int
destination.IntValue = DefaultValueConverter.ConvertToInt32(source.StringValue);

// int -> string
destination.StringValue = DefaultValueConverter.ConvertToString(source.IntValue);
```

The generator handles nullable values itself, calling the converter for a value only:

```csharp
// int? -> string
destination.StringValue = source.NullableValue is not null
    ? DefaultValueConverter.ConvertToString(source.NullableValue.GetValueOrDefault())
    : default!;
```

When a culture or a format applies, the overload taking the culture and the format is called instead ([Converter overloads taking the culture](#converter-overloads-taking-the-culture)).

### Other conversions

Types without a specialized method are converted by their own conversion operator, `Parse` (from a string, to a type implementing `IParsable<T>`, as `Parse(text, provider)`) or `ToString(format, provider)` (to a string), with the culture in effect, or the invariant culture when none applies ([Culture and formats](#culture-and-formats)). A number goes to a narrower numeric type by a cast, as C# casts it (`(int)source.LongValue`), and an enum by a switch over its members, or by a cast to or from a number.

A conversion none of these makes is reported (SMP0402), as it would fall back to the generic `Convert<TSource, TDestination>`, which is not AOT-safe, unless a `[ValueConverter]` class takes it ([Custom value converters](#custom-value-converters)). When the source and the target are classes, structs or collections, most likely a nested member or a collection mapped without its attribute, the message tells to use `[MapNested]` or `[MapCollection]`.

A user-defined conversion of a reference type is not lifted, so a value that may be null goes through the operator for a value only: `source.Email is not null ? (string)source.Email : null` for an `Email?` with `implicit operator string(Email email)`.

The conversions do not call a member obsolete as an error; another conversion takes over when there is one ([Obsolete members](#obsolete-members)).

### Enum conversions

An enum going to another enum matches the members by name. A value no member of the target has the name of, such as a combination of flags, gives `default`, or `null` to a nullable enum target. A string goes to an enum the same way, a string no member has the name of going through `Enum.Parse`, which throws for one it cannot parse, or to a nullable enum target through `Enum.TryParse`, which gives `null` for it (a number in the string still gives its value). An enum goes to a string as the member name (`ToString()` for a value no member has), and to and from a number by a cast. Strict mode reports the members of the source enum no member of the target enum has the name of (SMP0503).

An enum member marked `[Obsolete]` is written as a cast of its value in these switches ([Obsolete members](#obsolete-members)).

### Default formats

Without a culture and without a format (`DefaultCulture = Invariant`, no culture name and no `CultureInfo` parameter), a value goes to text and back as follows (`DefaultValueConverter`, under the invariant culture):

| Type | To `string` | From `string` |
|------|-------------|---------------|
| Numbers (`int`, `long`, `double`, `decimal`, `Half`, `Int128`, `BigInteger`, ...) | `ToString(CultureInfo.InvariantCulture)` | `Parse(text, CultureInfo.InvariantCulture)` |
| `bool` | `True` / `False` | `bool.Parse` |
| `char` | the character | `char.Parse` |
| `Guid` | `D` (`00000000-0000-0000-0000-000000000000`) | `Guid.Parse` |
| `DateTime` | `O`, the round-trip format (`2024-01-02T03:04:05.6780000Z`; the offset for a local time, nothing for an unspecified one) | `DateTime.Parse` with `DateTimeStyles.RoundtripKind`, which keeps the kind the text gives (UTC for a `Z`, local for an offset, unspecified without either) |
| `DateTimeOffset` | `O` (`2024-01-02T03:04:05.0000000+09:00`) | `DateTimeOffset.Parse` |
| `DateOnly` | `O` (`2024-01-02`) | `DateOnly.Parse` |
| `TimeOnly` | `O` (`03:04:05.0000000`) | `TimeOnly.Parse` |
| `TimeSpan` | `c` (`1.02:03:04.5000000`) | `TimeSpan.Parse` |
| Enums | the member name (`ToString()` for a value no member has) | by member name (otherwise `Enum.Parse`, or `Enum.TryParse` for a nullable target) |

The round-trip format keeps the fractions of a second and the kind of a `DateTime` (a `Z` for UTC, the offset for local). With a culture (a culture name, `DefaultCulture = Current`, or a `CultureInfo` parameter), the formats of the culture apply (`ToString(culture)`, `Parse(text, culture)`): without a format, `DateTime`, `DateTimeOffset`, `DateOnly` and `TimeOnly` are written in the general format of that culture, and a `TimeSpan` in the constant format `c`, which does not depend on the culture. `DateTimeFormat` / `NumberFormat` give the format ([Culture and formats](#culture-and-formats)).

### `DefaultValueConverter`

`DefaultValueConverter` is the value converter used when no `[ValueConverter]` is given. Each specialized method has two overloads: one without a culture, `ConvertTo{TargetType}(value)`, and one taking the culture and the format, `ConvertTo{TargetType}(value, IFormatProvider culture, string? format)`.

| Conversion | Without a culture | With the culture and the format |
|------------|-------------------|---------------------------------|
| `string` to a number (`Int32`, `Int64`, `Int16`, `Byte`, `SByte`, `UInt32`, `UInt64`, `UInt16`, `Single`, `Double`, `Decimal`, `Half`, `Int128`, `UInt128`, `BigInteger`) | `Parse(text, InvariantCulture)` | `Parse(text, culture)`; the format is not used, as the numeric `Parse` takes no format string |
| A number to `string` | `ToString(InvariantCulture)` | `ToString(culture)`, or `ToString(format, culture)` |
| `string` to `bool`, `char`, `Guid`, and these to `string` | `Parse`, `ToString()` | the same: the culture and the format are not used |
| `string` to `DateTime` | `DateTime.Parse(text, InvariantCulture, RoundtripKind)` | `DateTime.Parse(text, culture)`, or `DateTime.ParseExact(text, format, culture, ...)`, with `DateTimeStyles.RoundtripKind` for `O` / `o` |
| `string` to `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | `Parse(text, InvariantCulture)` | `Parse(text, culture)`, or `ParseExact(text, format, culture)` |
| `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` to `string` | `ToString("O", InvariantCulture)` | `ToString(culture)`, or `ToString(format, culture)` |
| `TimeSpan` to `string` | `ToString("c", InvariantCulture)` | `ToString(null, culture)`, or `ToString(format, culture)` |
| `int`, `long`, `float`, `double` to `Half` | a cast | a cast: the culture and the format are not used |

`Char` implements `IParsable<char>` / `ISpanParsable<char>` explicitly, so there is no public `Char.Parse(ReadOnlySpan<char>, IFormatProvider)` to call; the specialized `ConvertToChar` avoids that path.

With `DateTimeFormat = "O"` (or `"o"`), text goes to `DateTime` with `DateTimeStyles.RoundtripKind`, keeping the kind it gives, as without a format, where `DateTimeStyles.None` would turn a `Z` into local time. With `"R"` (or `"r"`), the RFC 1123 format, whose `GMT` is text of the format and not a time zone, `DateTime.ParseExact` gives the time as written, of an unspecified kind, as `ToString("R")` writes the time as it is, without converting it to UTC.

`Convert<TSource, TDestination>(value)` is the generic fallback, called only when no specialized method exists and a conversion is needed (not the same type, nor a value to or from its nullable type), after the generated code has handled the nullable values. The generator calls it only through a `[ValueConverter]`, as the `{Method}<TSource, TDestination>` of its class; without one, a conversion that would need it is reported (SMP0402). The one of `DefaultValueConverter` branches on the types, which the JIT resolves for each instantiation: it converts between the built-in numbers by casts and to `string` with the invariant culture, parses `string` into numbers, `bool`, `DateTime` (keeping the round-trip kind) and `Guid`, writes `bool`, `DateTime` (`O`) and `Guid` as text, and otherwise casts the value directly (for enums and compatible types), which boxes it.

### Custom value converters

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

- A converter method that is missing, such as the generic fallback of a conversion no specialized method covers, is reported (SMP0104).
- When a culture applies (a culture name, `DefaultCulture = Current` or a `CultureInfo` parameter) or a format, the converter has to provide the overload `(value, IFormatProvider, string?)` of each specialized method it uses (SMP0104 otherwise) ([Converter overloads taking the culture](#converter-overloads-taking-the-culture)).
- The methods of a converter class take values, and do not receive the custom parameters.
- A method of the converter class obsolete as an error is not called: another conversion takes over when there is one (the generic method for a specialized one), and otherwise it is reported as not matching (SMP0104). One obsolete as a warning is called.
- `[ValueConverter]` on the class is reported at that attribute for a value it gives.

### Converter priority

Priority order (highest to lowest):

| Level | Scope |
|-------|-------|
| `[MapProperty(Converter = nameof(...))]` | Single property |
| `[ValueConverter]` on mapper method | All properties of that method |
| `[ValueConverter]` on class | All mapper methods in the class |
| `DefaultValueConverter` | Fallback |

The `Converter` of `[MapProperty]` takes the source member as the method of `[MapUsing]` takes the source, also as a type it converts to implicitly, or as the struct a nullable struct holds ([Taking the value](#taking-the-value)), and returns the target type or a type converting to it implicitly ([Return values](#return-values)).

---

## Culture and formats

The conversions between values and text use a culture: the specialized methods of the value converter, the `Parse` of types implementing `IParsable<T>`, and the `ToString(format, provider)` of other types.

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
    public static partial Dest3 Map(Src3 src, CultureInfo culture);   // the caller gives the culture
}
```

### Default culture

`DefaultCulture` of `[MapperProfile]`, on the class or the assembly, is the culture used when no culture name applies to a conversion and the method has no `CultureInfo` parameter:

| `MapperCulture` | Conversions |
|-----------------|-------------|
| `Invariant` (default) | The culture-neutral conversions of the value converter, its overloads without a culture: numbers with the invariant culture, dates and times in the ISO 8601 round-trip format `O`, and `TimeSpan` in `c`, as in [Default formats](#default-formats) |
| `Current` | `CultureInfo.CurrentCulture`, read at each conversion, through the overloads of the converter taking the culture, exactly as a culture name does: numbers with that culture, and dates and times in its general format (`ToString(culture)`; a `TimeSpan` in `c`) |

```csharp
// The conversions without a culture name use the culture of the thread running the mapper
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current)]
```

The `DefaultCulture` of the class profile wins over the one of the assembly profile, and `Invariant` applies when neither sets it. `[Mapper]` has no `DefaultCulture`; a method takes a culture of its own with `Culture` or a `CultureInfo` parameter.

### Culture names

`Culture` of `[MapProperty]`, `[Mapper]` and `[MapperProfile]` names a culture, such as `"ja-JP"`: subtags of letters and digits separated by hyphens, such as `ja-JP` or `zh-Hant-TW` (a language of 1 to 8 letters, and subtags of 1 to 8 letters and digits), with an alternate sort order after an underscore, such as `de-DE_phoneb` (SMP0404 otherwise). The culture names the field the generated code keeps it in; whether it exists depends on the system the mapper runs on, and is not checked.

With a culture name, the conversions go through the overloads of the converter taking the culture. The resolved `CultureInfo` is cached as a `static readonly` field in the generated class, to avoid repeated `GetCultureInfo(...)` calls.

### `CultureInfo` parameter

A custom parameter of type `System.Globalization.CultureInfo` gives the culture of the conversions of the method, through the overloads of the converter taking the culture:

```csharp
internal static partial class OrderMappers
{
    [Mapper]
    public static partial OrderDto Map(Order source, CultureInfo culture);
}

var dto = OrderMappers.Map(order, CultureInfo.GetCultureInfo("fr-FR"));
```

It is still a custom parameter: it is passed on, like any other, to the methods of `[MapUsing]`, `[MapCondition]`, `[BeforeMap]` and `[AfterMap]` and the `Converter` of `[MapProperty]` that take the custom parameters ([Custom parameters](#custom-parameters)). It goes to the mappers of `[MapNested]` / `[MapCollection]` that declare a `CultureInfo` parameter as well, so the nested objects and the elements are converted with it; a nested mapper without one converts with the culture it resolves itself. Of several `CultureInfo` parameters, the one named `culture`, as the culture parameter of the converter is named, gives the culture of the conversions of the method, and each goes to the methods by its name; a mapper with several and none named `culture` is reported (SMP0406).

A nullable parameter (`CultureInfo?`), or one declared with nullable annotations disabled, that is null at run time gives the culture the method would use without the parameter: the field of its culture name, the current culture under `DefaultCulture = Current`, or the invariant culture, still through the overloads taking the culture. With the invariant culture, a date then takes the general format of the invariant culture (`01/02/2024 03:04:05`), not the round-trip format `O`.

`Culture` of `[Mapper]` on a method with a `CultureInfo` parameter is not used, as the parameter gives the culture, and is reported as a warning at the `[Mapper]` (SMP0405). The `Culture` of a profile is a default, which the parameter takes the place of without a warning, and the `Culture` of `[MapProperty]` still applies to its mapping.

### Culture precedence

The culture of a conversion is the first of:

1. `Culture` of `[MapProperty]`
2. the `CultureInfo` parameter of the method
3. `Culture` of `[Mapper]`
4. `Culture` of the `[MapperProfile]` of the class
5. `Culture` of the `[MapperProfile]` of the assembly
6. `DefaultCulture` of the class profile, then of the assembly profile (`Invariant` when neither sets it)

### Formats

`DateTimeFormat` and `NumberFormat` of `[MapProperty]`, `[Mapper]` and `[MapperProfile]` give the format of the conversions between values and text. Each is resolved on its own, `[MapProperty]` over `[Mapper]` over the class profile over the assembly profile, and independently of the culture: under `[MapperProfile(Culture = "ja-JP", NumberFormat = "N0")]`, `[Mapper(NumberFormat = "N2")]` formats with ja-JP and N2, and `[Mapper(Culture = "de-DE")]` with de-DE and N0.

A format does not need a culture name: without one, it applies with the culture in effect, the invariant culture under `Invariant`, the current culture under `Current`, or the `CultureInfo` parameter. A conversion with a format goes through the overload of the converter taking the culture and the format.

- `DateTimeFormat` is the format of the conversions of `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan` to and from `string`; `NumberFormat` is the format of the other conversions, numbers and the `ToString(format, provider)` of other types. `DefaultValueConverter` writes numbers with `NumberFormat`, and parses text into a number with the culture only, as the numeric `Parse` takes no format.
- The `DateTimeFormat` of `[Mapper]` and of a profile applies to every date and time type the mapper converts, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly` and `TimeSpan` alike. To format them differently, give `DateTimeFormat` on `[MapProperty]` for each. A `TimeSpan` format is written differently from the others (`hh\:mm`, with the separators escaped), and one meant for dates fails for a `TimeSpan` or a `TimeOnly` at run time (`FormatException`).
- Text goes to a date or a time with a format through `ParseExact`. With `DateTimeFormat = "O"` (or `"o"`), text goes to `DateTime` with `DateTimeStyles.RoundtripKind` as well, keeping the kind it gives; with `"R"` (or `"r"`), whose `GMT` is text of the format and not a time zone, `DateTime.ParseExact` gives the time as written, of an unspecified kind, as `ToString("R")` writes the time as it is, without converting it to UTC.

### Converter overloads taking the culture

When a culture applies (a culture name, `DefaultCulture = Current`, or a `CultureInfo` parameter) or a format, the specialized methods of the converter are called through their overload taking the culture and the format, `(value, IFormatProvider, string?)`, as in `DefaultValueConverter.ConvertToString(int source, IFormatProvider culture, string? format)`; the format is `null` when none applies. A custom `[ValueConverter]` has to provide that overload for each specialized method it uses then (SMP0104 otherwise). Its culture parameter takes a `CultureInfo`, as that type or a type it converts to (such as `IFormatProvider`), and its format parameter a `string`.

---

## Profiles

`[MapperProfile]` sets the defaults of the mapper methods: on a class or a struct for the mapper methods declared in it, and on the assembly (`[assembly: MapperProfile(...)]`, in any file of the project) for all the mapper methods of the assembly.

```csharp
[assembly: MapperProfile(DefaultCulture = MapperCulture.Current, NameComparison = StringComparison.OrdinalIgnoreCase)]

[MapperProfile(Strict = true, NumberFormat = "N2")]
internal static partial class InvoiceMappers
{
    [Mapper]                                         // strict, N2, current culture, case-insensitive names
    public static partial InvoiceDto Map(Invoice source);

    [Mapper(Strict = false, NumberFormat = "N0")]    // not strict, N0
    public static partial InvoiceSummary Summarize(Invoice source);
}
```

All the settings of a profile, `Strict`, `NameComparison`, `DefaultCulture`, `Culture`, `DateTimeFormat` and `NumberFormat`, are defaults: the setting of `[Mapper]` wins over the one of the class profile, which wins over the one of the assembly profile, each setting resolved on its own. A setting given explicitly wins, also `Strict = false` under a strict profile. `DefaultCulture` is set on profiles only. The `Culture`, `DateTimeFormat` and `NumberFormat` of `[MapProperty]` win over all of them for its mapping, and a `CultureInfo` parameter over the cultures of `[Mapper]` and the profiles ([Culture precedence](#culture-precedence)).

A diagnostic about a value a profile gives, such as a `Culture` that is not a culture name (SMP0404), is reported at that profile.

---

## Callbacks and conditions

### Before and after callbacks

```csharp
[Mapper]
[BeforeMap(nameof(BeforeMapping))]
[AfterMap(nameof(AfterMapping))]
public static partial void Map(Source source, Destination destination);

private static void BeforeMapping(Source source, Destination destination) { /* ... */ }
private static void AfterMapping(Source source, Destination destination) { /* ... */ }
```

- `[BeforeMap]` calls its method before the assignments, and `[AfterMap]` after them ([Assignment order](#assignment-order)).
- A callback is found as the other [methods named by attributes](#methods-named-by-attributes) are: in an instance mapper it may be an instance method, and it is not generic.
- A callback takes the source and the destination as their types, or, by value, as base classes or interfaces they convert to, so that one callback serves several mappers: `private static void Audit(IEntity source, IAuditable destination)`. A struct source or destination goes to its own type only, as a boxed copy would take what the callback writes. The custom parameters follow them, as their own types, when the callback declares them.
- Of the overloads, the one the call binds to is used, as C# binds it, and an ambiguous call, or one binding to a method not matched, is reported (SMP0102 / SMP0103), as is a callback that does not match `(Source, Destination)` followed by the custom parameters it declares.

### Conditional mapping

The destination property is assigned only when the condition method, which takes the source value (and the custom parameters), returns `true`.

```csharp
[Mapper]
[MapCondition(nameof(Destination.Name), nameof(ShouldMapName))]
public static partial void Map(Source source, Destination destination);

private static bool ShouldMapName(string? name) => !string.IsNullOrEmpty(name);
```

- A condition guards the property mapping of its target: the automatic one, or a `[MapProperty]`, also through a dotted path. On a target no property mapping assigns, one nothing maps, one `[MapIgnore]` leaves out, or one `[MapConstant]`, `[MapExpression]`, `[MapUsing]`, `[MapFrom]`, `[MapNested]` or `[MapCollection]` assigns, it would do nothing, and is reported (SMP0221). A target that is not found is reported (SMP0214): it has to be a property or a field of the destination, a dotted path of them, or a parameter of the constructor a return-type mapper calls.
- The method is found and matched as the other [methods named by attributes](#methods-named-by-attributes) are: in an instance mapper it may be an instance method, it takes the source value as described in [Taking the value](#taking-the-value), and it returns `bool`. Of the overloads, the one the call binds to is used, and an ambiguous call, or one that does not match, is reported (SMP0106).
- A condition whose parameter does not take null (a reference annotated as not null, or `[DisallowNull]`) is not given a null source, which does not meet it: `if (source.Name is not null && IsShort(source.Name))`. A condition taking the struct a nullable struct source holds, or a type it converts to implicitly, gets its `Value`, and a null source does not meet it: `if (source.Count is not null && IsPositive(source.Count.Value))`.
- When an intermediate member of a dotted source is null, the target is left as it is, as there is no source value to test.
- A member assigned through a constructor or an object initializer cannot be left unassigned, so `[MapCondition]` is rejected there (SMP0215).

---

## Strict mode

`Strict = true` on `[Mapper]`, or on a [profile](#profiles), enables warnings about the mapping:

```csharp
[Mapper(Strict = true)]
public static partial Destination Map(Source source);
```

### Unmapped members

A destination property no mapping assigns is reported (SMP0501, a warning): one the mapper can assign through a setter, or through an `init` accessor for a return-type mapper, which sets it in the object initializer, that neither the automatic mapping nor an attribute maps, and `[MapIgnore]` does not leave out. A member a dotted target path writes into, such as `Child` for `[MapProperty("Child.Value", ...)]`, is mapped through the path.

For a return-type mapper, so is a property only a constructor can set (get-only, or with a setter the mapper class cannot call) that a parameter of a constructor the mapper can call takes, when the construction chosen passes it no argument ([Choosing the constructor](#choosing-the-constructor)), whether the source has it or not. A property no constructor takes, such as a computed one, is not reported, nor, for a void mapper, which never constructs, is one only a constructor sets or an `init`-only one. One an optional parameter left out would set is reported as well, the constructor giving it the default value; a `[MapIgnore]` naming it leaves it to that value without the warning. An `[Obsolete]` property, which the automatic mapping leaves out, is not reported ([Obsolete members](#obsolete-members)).

### Values that may be null

A mapping giving a value that may be null to a target that does not take null, which gets `null` or `default` for it, is reported as well (SMP0502, a warning), unless the mapping says what the target gets with `NullValue`, `NullBehavior.Skip` or a `[MapCondition]`.

The value may be null when it is declared so:

- a source member of a nullable type, or with `[MaybeNull]` on the property or the return of its getter;
- one read through a member of a nullable type where the value is made as an expression (a constructor argument or an object initializer entry; a statement leaves the target as it is then);
- a nullable reference the method of `[MapFrom]` or `[MapUsing]`, a converter, or the mapper of `[MapNested]` / `[MapCollection]` returns, which the generated code takes with `!` (not when the method gets a value that is not null and `[return: NotNullIfNotNull]` of its first parameter says it returns one for it, as a generated mapper declares);
- a source of `[MapNested]` / `[MapCollection]` or an element of a nullable type that a mapper not taking null is not called for.

A target that does not take null is a struct, or a reference annotated as not null without `[AllowNull]`, or one with `[DisallowNull]`. A reference declared with nullable annotations disabled, which the null checks take as nullable ([Nullable annotations disabled](#nullable-annotations-disabled)), is not taken as one here: it says nothing about null, and a model written without the annotations would get the warning for every such member.

A return-type mapper whose source is declared nullable returning a type that does not take null, as in `Dst Map(Src? source)`, returns `default` for a null source, which is reported at the method, as the target `(return)`.

### Enum members without a match

A mapping of an enum to another enum, which matches the members by name, is reported when the source enum has members no member of the target enum has the name of, whose values give the target `default`, or `null` for a nullable enum (SMP0503, a warning, listing the members). The values of a `[Flags]` enum combining members are not known before they come, so only the members are looked at, and a converter given to the mapping takes over the conversion, so it is not reported.

### Suppressing the warnings

SMP0501 is reported at the mapper method, as the errors about the construction of its destination are, and SMP0502 and SMP0503 at the attribute of the mapping, or at the method for the automatic mapping ([Where diagnostics are reported](#where-diagnostics-are-reported)). A warning reported at the method starts at its first attribute, so a `#pragma warning disable` suppresses it only when it comes before the attributes of the method, and one reported at an attribute only when it comes before that attribute; one between the attributes and the method does not. `[SuppressMessage]` on the method suppresses both:

```csharp
#pragma warning disable SMP0501
[Mapper(Strict = true)]
public static partial Destination Map(Source source);       // suppressed
#pragma warning restore SMP0501

[Mapper(Strict = true)]
#pragma warning disable SMP0501
public static partial Destination MapOther(Source source);  // not suppressed: the warning starts at [Mapper]
#pragma warning restore SMP0501

[SuppressMessage("Usage", "SMP0501")]
[Mapper(Strict = true)]
public static partial Destination MapThird(Source source);  // suppressed
```

---

## NativeAOT and trimming

Smart.Mapper is fully compatible with NativeAOT and IL trimming.

- `<IsAotCompatible>true</IsAotCompatible>` is declared in `Smart.Mapper.csproj`
- All type conversions are handled through specialized methods - no generic reflection fallback at runtime; a conversion that would need the generic fallback is reported (SMP0402)
- The generated code never uses `Activator.CreateInstance`; object creation is expanded inline by the generator (the `Action` overloads of `DefaultCollectionConverter`, which create elements with `new()`, are marked `RequiresUnreferencedCode` and `RequiresDynamicCode`)
- `[DynamicallyAccessedMembers]` annotations (public methods and public nested types) are applied to `ValueConverterAttribute.ConverterType` and `CollectionConverterAttribute.ConverterType`

> **`[MapExpression]` warning** - If an expression contains reflection APIs (`Activator`, `Type.GetType`, `Assembly.Load`, `MethodInfo`, `PropertyInfo`, `FieldInfo`, `RuntimeHelpers.GetUninitializedObject`, `MakeGenericType`, `MakeGenericMethod`), SMP0403 is emitted at the attribute. Prefer `[MapFrom]` or `[MapUsing]` in AOT contexts.

---

## Diagnostics

The generator reports compile-time diagnostics with IDs in phase-based bands: SMP00xx for the mapper method, SMP01xx for the mapping attributes and the methods they name, SMP02xx for the member mapping features, SMP03xx for construction, SMP04xx for conversion and AOT, and SMP05xx for strict mode. See [Diagnostics.md](../Diagnostics.md) for the cause of each diagnostic and how to fix it.

### Where diagnostics are reported

A diagnostic whose cause is an attribute is reported at that attribute: the second of two attributes that contradict each other (SMP0101), the `[MapperProfile]` (of the class or the assembly) or the class's `[ValueConverter]` for a value it gives, the attribute naming an instance method a static mapper cannot call (SMP0107), and the `[Mapper]` whose `Culture` a `CultureInfo` parameter takes over (SMP0405). One about the method itself, the automatic mapping, the construction of the destination (SMP0301, SMP0303, SMP0304, SMP0305) or the unmapped properties of strict mode (SMP0501) is reported at the mapper method, and the other warnings of strict mode (SMP0502, SMP0503) at the attribute of the mapping, or at the method for the automatic mapping. A warning reported at the method starts at its first attribute, so a `#pragma warning disable` suppresses it only when it comes before the attributes ([Suppressing the warnings](#suppressing-the-warnings)).

A mapper reported with an error gets an implementation throwing `NotImplementedException` in place of none, so that the error is not accompanied by the missing implementation (CS8795); the build fails on the error, so the implementation never runs. A method not `partial`, or in a type not `partial` or file-local (SMP0001), which the generated code could not implement, gets none; one declared without an accessibility modifier gets one without it, as it is declared.

### Diagnostic list

| ID | Description | Severity |
|----|-------------|----------|
| SMP0001 | Mapper method must be `partial`, in types that are all `partial` and none file-local (`file`) | Error |
| SMP0002 | Mapper method has no parameter, or is `void` without a destination parameter after the source | Error |
| SMP0004 | Mapper parameter name starts with `__` (reserved for the generated code) | Error |
| SMP0005 | Parameter has a modifier the generated code cannot work with (`out`, or none, `in` or `ref readonly` on the struct destination of a void mapper, which is taken by `ref`) | Error |
| SMP0006 | Source parameter is a nullable value type, which has none of the members of the struct it holds | Error |
| SMP0007 | Source or destination is a collection, an array or a tuple, or a type parameter constrained to one, which a mapper does not map as a whole (map the elements with a mapper of the element type, or a type holding the collection with `[MapCollection]`) | Error |
| SMP0008 | Mapper method returns by reference (`ref` / `ref readonly`), which cannot return the destination it creates | Error |
| SMP0101 | Several mapping attributes target the same destination property or a member and a member of it (`Child` and `Child.Value`), or `[MapIgnore]` and a mapping attribute name the same target | Error |
| SMP0102 | `BeforeMap` method signature does not match | Error |
| SMP0103 | `AfterMap` method signature does not match | Error |
| SMP0104 | Converter method is not found or its signature does not match (for a `[ValueConverter]` class, also the overload taking the culture and the format when a culture or a format applies) | Error |
| SMP0105 | Converter return type does not convert implicitly to the target property type | Error |
| SMP0106 | Property condition method signature does not match | Error |
| SMP0107 | A static mapper names a method the mapper class has only as instance methods, which it cannot call | Error |
| SMP0201 | `MapUsing` method signature does not match | Error |
| SMP0202 | `MapUsing` return type does not convert implicitly to the target property type | Error |
| SMP0203 | `[MapFrom]` target property is not found on the destination type | Error |
| SMP0204 | `MapFrom` member is not a parameterless method the mapper can call or a property path of the source type (inherited ones included) | Error |
| SMP0205 | `MapFrom` member type does not convert implicitly to the target property type | Error |
| SMP0206 | `[MapCollection]` / `[MapNested]` source property is not found (the source is a property of the source type, not a dotted path) | Error |
| SMP0207 | `[MapCollection]` / `[MapNested]` target property is not found | Error |
| SMP0208 | `[MapCollection]` source property is not a collection type | Error |
| SMP0209 | `[MapCollection]` target property is not a collection type | Error |
| SMP0210 | `MapCollection` element mapper method is not found or its signature does not match, or `Mapper` is not specified | Error |
| SMP0211 | `MapNested` mapper method is not found or its signature does not match, or `Mapper` is not specified | Error |
| SMP0212 | `[MapCollection]` / `[MapNested]` target cannot be assigned (no setter or `init` accessor the mapper can call, or `init`-only in a void mapper; `InPlace` refills the instance it holds) | Error |
| SMP0213 | `[MapProperty]` source property is not found | Error |
| SMP0214 | Mapping target is not found or cannot be assigned (no setter the mapper can call, a `readonly` field, a dotted path the generated code cannot go through, such as one through a nullable struct), including the target of `[MapIgnore]` / `[MapCondition]` | Error |
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
| SMP0402 | Not AOT-safe: the conversion may fall back to the generic `Convert<TSource, TDestination>` (for a class, a struct or a collection, the message tells to use `[MapNested]` / `[MapCollection]`) | Error |
| SMP0403 | AOT warning: `MapExpression` may contain a reflection pattern | Warning |
| SMP0404 | `Culture` is not a culture name | Error |
| SMP0405 | `Culture` of `[Mapper]` is not used, as a `CultureInfo` parameter of the method gives the culture | Warning |
| SMP0406 | The mapper has several `CultureInfo` parameters and none named `culture`, which gives the culture of the conversions | Error |
| SMP0501 | Strict mode: a destination property the mapper can assign, or one only a constructor the mapper can call sets, is not mapped (reported at the mapper method) | Warning |
| SMP0502 | Strict mode: a value that may be null goes to a target that does not take null, which gets `null` or `default` for it, without `NullValue`, `NullBehavior.Skip` or `[MapCondition]` (reported at the attribute, or at the mapper method for the automatic mapping), or a return-type mapper whose source is declared nullable returns a type that does not take null (reported at the mapper method, as the target `(return)`) | Warning |
| SMP0503 | Strict mode: members of the source enum have no member of the same name in the target enum a mapping by name goes to (reported at the attribute, or at the mapper method for the automatic mapping) | Warning |
