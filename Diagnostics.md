# Diagnostics

## Mapper method

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0001 | ❌ Error | Mapper method is not `static partial` | Declare the method as `static partial` |
| SMP0002 | ❌ Error | Mapper method does not have 1 parameter (return pattern) or 2 parameters (void pattern) | Adjust the parameter list to one of the supported patterns |
| SMP0003 | ❌ Error | Two custom parameters have the same type | Give each custom parameter a distinct type |

## Mapping attributes

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0101 | ❌ Error | Multiple mapping attributes target the same destination property | Leave a single mapping attribute per destination property |
| SMP0102 | ❌ Error | `BeforeMap` method does not match `(Source, Destination)` or `(Source, Destination, customParams...)` | Correct the `BeforeMap` method signature |
| SMP0103 | ❌ Error | `AfterMap` method does not match `(Source, Destination)` or `(Source, Destination, customParams...)` | Correct the `AfterMap` method signature |
| SMP0104 | ❌ Error | Converter method does not match `(SourceType)` or `(SourceType, customParams...)` returning the target property type | Correct the converter method signature |
| SMP0105 | ❌ Error | Converter parameter types match but the return type does not match the target property type | Change the converter return type to the target property type |
| SMP0106 | ❌ Error | Property condition method does not match `(SourceType)` or `(SourceType, customParams...)` returning `bool` | Correct the condition method signature |

## Member mapping

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0201 | ❌ Error | `MapUsing` method does not match `(Source)` or `(Source, customParams...)` returning the target property type | Correct the `MapUsing` method signature |
| SMP0202 | ❌ Error | `MapUsing` return type does not match the target property type | Change the `MapUsing` return type to the target property type |
| SMP0203 | ❌ Error | `MapFrom` target property is not found on the destination type | Correct the target name, or add the property to the destination type |
| SMP0204 | ❌ Error | `MapFrom` member is not a parameterless method or a property path on the source type | Point `MapFrom` at a parameterless method or a property path |
| SMP0205 | ❌ Error | `MapFrom` member type does not match the target property type | Align the member type with the target property type |
| SMP0206 | ❌ Error | Source property given to `[MapCollection]` / `[MapNested]` is not found on the source type | Correct the source name, or add the property to the source type |
| SMP0207 | ❌ Error | Target property given to `[MapCollection]` / `[MapNested]` is not found on the destination type | Correct the target name, or add the property to the destination type |
| SMP0208 | ❌ Error | `[MapCollection]` source property is not a collection type | Use an `IEnumerable<T>` implementation, `Memory<T>` or `ReadOnlyMemory<T>` |
| SMP0209 | ❌ Error | `[MapCollection]` target property is not a collection type | Use an `IEnumerable<T>` implementation for the target property |
| SMP0210 | ❌ Error | `MapCollection` element mapper method is not found or its signature does not match | Correct the element mapper name and signature |
| SMP0211 | ❌ Error | `MapNested` mapper method is not found or its signature does not match | Correct the mapper name and signature |
| SMP0212 | ❌ Error | `[MapCollection]` / `[MapNested]` targets an init-only or required member, which the generated loop cannot assign | Use a settable property for the target |
| SMP0213 | ❌ Error | `[MapProperty]` source property is not found on the source type | Correct the source name, or add the property to the source type |
| SMP0214 | ❌ Error | `[MapProperty]` target property is not found on the destination type, or has no setter and is not assigned by a constructor | Correct the target name, or make the property assignable |
| SMP0215 | ❌ Error | `[MapCondition]` or `NullBehavior.Skip` is applied to a member assigned through a constructor or object initializer | Remove the option, or assign the member through a property |
| SMP0216 | ❌ Error | `[MapIgnore]` is applied to a member that a constructor requires a value for | Remove `[MapIgnore]`, or provide the value explicitly |

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
| SMP0402 | ❌ Error | Property has no specialized conversion and falls back to `Convert<TSource, TDestination>`, which is not AOT-safe | Provide a specialized conversion, or apply `[ValueConverter(typeof(...))]` |
| SMP0403 | ⚠️ Warning | `MapExpression` contains a reflection pattern that may not be AOT-compatible | Avoid reflection in the expression, or use `[MapFrom]` / `[MapUsing]` |

## Strict mode

| ID | Severity | Description | How to fix |
|---|---|---|---|
| SMP0501 | ⚠️ Warning | Destination property is not mapped while strict mode is enabled | Add a mapping attribute, or `[MapIgnore]` |
