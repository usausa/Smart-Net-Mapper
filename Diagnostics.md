# Diagnostics

## Mapper method

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0001 | ❌ Error | `[Mapper]` method is not `static partial` | Declare the method as `static partial` |
| SMP0002 | ❌ Error | `[Mapper]` method does not have 1 parameter (return pattern) or 2 parameters (void pattern) | Adjust the parameter list to one of the supported patterns |
| SMP0003 | ❌ Error | `[Mapper]` has two custom parameters of the same type | Give each custom parameter a distinct type |
| SMP0004 | ❌ Error | `[Mapper]` parameter name starts with `__`, which is reserved for the names the generated code declares | Rename the parameter so that it does not start with `__` |
| SMP0005 | ❌ Error | `[Mapper]` parameter has a modifier the generated code cannot work with: `out` on any parameter, or `in` / `ref readonly` on a struct destination of a void mapper | Remove the modifier; pass a struct destination by `ref` |

## Mapping attributes

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0101 | ❌ Error | Multiple mapping attributes target the same destination property | Leave a single mapping attribute per destination property |
| SMP0102 | ❌ Error | `[BeforeMap]` method does not match `(Source, Destination)` or `(Source, Destination, customParams...)` | Correct the `BeforeMap` method signature |
| SMP0103 | ❌ Error | `[AfterMap]` method does not match `(Source, Destination)` or `(Source, Destination, customParams...)` | Correct the `AfterMap` method signature |
| SMP0104 | ❌ Error | Converter method is not found, or does not match `(SourceType)` or `(SourceType, customParams...)` returning the target property type | Add the converter method, or correct its signature |
| SMP0105 | ❌ Error | Converter parameter types match but the return type does not match the target property type | Change the converter return type to the target property type |
| SMP0106 | ❌ Error | Property condition method does not match `(SourceType)` or `(SourceType, customParams...)` returning `bool` | Correct the condition method signature |

These methods, `[MapUsing]` methods (SMP0201) and the mapper methods of `[MapCollection]` / `[MapNested]` (SMP0210 / SMP0211) also do not match when a parameter modifier cannot take its argument. No modifier and `in` take any argument, `ref readonly` takes a variable, `ref` takes a writable variable, and `out` takes none. The arguments are:

- Values: property values, the elements of an `IList<T>` / `IReadOnlyList<T>` (which its indexer returns), and the format string
- Writable variables: parameters of the mapper taken by value or by `ref`, the instance the generated code creates, and the elements of an array, `List<T>` or `Memory<T>`
- Read-only variables: `in` / `ref readonly` parameters of the mapper, the elements of any other collection (`ImmutableArray<T>`, `ReadOnlyMemory<T>`, or read with `foreach`), and the culture given to a `CultureInfo` parameter (given to `IFormatProvider` or `object`, the culture is a value)

An element mapper given to a collection converter is passed as a delegate, so its parameters have to match those of the delegate (by value for `Func` / `Action`). The methods of a `[ValueConverter]` class (including the overload taking the culture and the format, called when `Culture` is set) and of a `[CollectionConverter]` class take values, and are reported the same way with SMP0104.

A converter class that lacks a method the generated code calls is reported with SMP0104 as well, instead of a compile error in the generated code:

- With `Culture`, the overload `(SourceType, culture, format)` of each specialized method used, whose culture parameter takes a `CultureInfo` (such as `IFormatProvider`) and whose format parameter takes a `string`
- The generic `Convert<TSource, TDestination>` (named by `Method` of `[ValueConverter]`) that a user converter falls back to
- The method of a collection converter for the target (`ToList`, `ToArray`, `ToHashSet`, ... by the target type, or the `Converter` of `[MapCollection]`), called as `Method<TSourceElement, TTargetElement>(source, mapper)`: it has to take the source collection, meet the constraints of its type parameters, and return a collection the target property takes

A void mapper of `[MapCollection]` / `[MapNested]` fills an instance the generated code creates with `new()`, so it matches only when the target type allows that (a struct, or a class that is not abstract with a constructor callable without arguments and no required members that constructor leaves unset); otherwise SMP0210 / SMP0211 are reported, unless a returning mapper of the same name can be used.

## Member mapping

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0201 | ❌ Error | `[MapUsing]` method does not match `(Source)` or `(Source, customParams...)` returning the target property type | Correct the `MapUsing` method signature |
| SMP0202 | ❌ Error | `[MapUsing]` return type does not match the target property type | Change the `MapUsing` return type to the target property type |
| SMP0203 | ❌ Error | `[MapFrom]` target property is not found on the destination type | Correct the target name, or add the property to the destination type |
| SMP0204 | ❌ Error | `[MapFrom]` member is not a parameterless method or a property path on the source type | Point `MapFrom` at a parameterless method or a property path |
| SMP0205 | ❌ Error | `[MapFrom]` member type does not match the target property type | Align the member type with the target property type |
| SMP0206 | ❌ Error | Source property given to `[MapCollection]` / `[MapNested]` is not found on the source type | Correct the source name, or add the property to the source type |
| SMP0207 | ❌ Error | Target property given to `[MapCollection]` / `[MapNested]` is not found on the destination type | Correct the target name, or add the property to the destination type |
| SMP0208 | ❌ Error | `[MapCollection]` source property is not a collection type | Use an `IEnumerable<T>` implementation, `Memory<T>` or `ReadOnlyMemory<T>` |
| SMP0209 | ❌ Error | `[MapCollection]` target property is not a collection type | Use an `IEnumerable<T>` implementation for the target property |
| SMP0210 | ❌ Error | `[MapCollection]` element mapper method is not found or its signature does not match | Correct the element mapper name and signature |
| SMP0211 | ❌ Error | `[MapNested]` mapper method is not found or its signature does not match | Correct the mapper name and signature |
| SMP0212 | ❌ Error | `[MapCollection]` / `[MapNested]` targets a member without a setter the mapper can call, or an init-only or required member, which the generated code cannot assign | Use a settable property for the target |
| SMP0213 | ❌ Error | `[MapProperty]` source property is not found on the source type | Correct the source name, or add the property to the source type |
| SMP0214 | ❌ Error | `[MapProperty]` target property is not found on the destination type, or has no setter and is not assigned by a constructor | Correct the target name, or make the property assignable |
| SMP0215 | ❌ Error | `[MapCondition]` or `NullBehavior.Skip` is applied to a member assigned through a constructor or object initializer | Remove the option, or assign the member through a property |
| SMP0216 | ❌ Error | `[MapIgnore]` is applied to a member that a constructor requires a value for | Remove `[MapIgnore]`, or provide the value explicitly |
| SMP0217 | ❌ Error | `[MapCollection]` target cannot take the collection the generated code creates for it: the `List<T>`, array, set or immutable collection the loop builds, or the `List<T>` / `HashSet<T>` that `CollectionStrategy.InPlace` creates when the target is null | Declare the target as a type that collection can be assigned to (such as `List<T>`, `IList<T>`, `IReadOnlyList<T>` or `ISet<T>`), or, without `InPlace`, build it with a collection converter |

## Construction

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0301 | ❌ Error | Constructor parameter has no matching source property | Add a `[MapProperty]` for the parameter, or provide a matching source property |
| SMP0302 | ❌ Error | Destination type has init-only or constructor-only members that a void mapper can never assign | Use a return-type mapper |
| SMP0303 | ❌ Error | Required destination property has no mapping | Add a mapping attribute, or `[MapIgnore]` |

## Conversion / AOT

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0401 | ❌ Error | `DateTimeFormat` or `NumberFormat` is specified but `Culture` is not set | Set `Culture` together with the format |
| SMP0402 | ❌ Error | Property has no specialized conversion (to a `string`, nor a `ToString(string, IFormatProvider)` method) and falls back to `Convert<TSource, TDestination>`, which is not AOT-safe | Provide a specialized conversion, or apply `[ValueConverter(typeof(...))]` |
| SMP0403 | ⚠️ Warning | `[MapExpression]` contains a reflection pattern that may not be AOT-compatible | Avoid reflection in the expression, or use `[MapFrom]` / `[MapUsing]` |

## Strict mode

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0501 | ⚠️ Warning | Destination property is not mapped while strict mode is enabled | Add a mapping attribute, or `[MapIgnore]` |
