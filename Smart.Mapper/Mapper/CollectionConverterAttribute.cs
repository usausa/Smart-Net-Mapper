namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies a custom collection converter class for the mapper.
/// Can be applied at class level or method level.
/// Method level takes precedence over class level.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class CollectionConverterAttribute : Attribute
{
    /// <summary>
    /// Gets the converter type.
    /// The type must have static methods with signatures:
    /// <code>TDest[]? ToArray&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Func&lt;TSource, TDest&gt; mapper)</code>
    /// <code>List&lt;TDest&gt;? ToList&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Func&lt;TSource, TDest&gt; mapper)</code>
    /// For void mappers:
    /// <code>TDest[]? ToArray&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Action&lt;TSource, TDest&gt; mapper) where TDest : new()</code>
    /// <code>List&lt;TDest&gt;? ToList&lt;TSource, TDest&gt;(IEnumerable&lt;TSource&gt;? source, Action&lt;TSource, TDest&gt; mapper) where TDest : new()</code>
    /// </summary>
    public Type ConverterType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectionConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The converter type.</param>
    public CollectionConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
