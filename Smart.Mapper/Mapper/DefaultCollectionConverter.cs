namespace Smart.Mapper;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Default collection converter for collection property mappings.
/// </summary>
public static class DefaultCollectionConverter
{
    /// <summary>
    /// Converts a collection to an array using the specified mapper function.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDest">The destination element type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="mapper">The mapper function for each element.</param>
    /// <returns>The converted array, or default if source is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToArray();
    }

    /// <summary>
    /// Converts a collection to a List using the specified mapper function.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDest">The destination element type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="mapper">The mapper function for each element.</param>
    /// <returns>The converted list, or default if source is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToList();
    }

    /// <summary>
    /// Converts a collection to an array using the specified void mapper action.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDest">The destination element type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="mapper">The mapper action for each element.</param>
    /// <returns>The converted array, or default if source is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static TDest[]? ToArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToArray();
    }

    /// <summary>
    /// Converts a collection to a List using the specified void mapper action.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TDest">The destination element type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="mapper">The mapper action for each element.</param>
    /// <returns>The converted list, or default if source is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static List<TDest>? ToList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToList();
    }

    // ============================================================
    // Array / List / ReadOnlySpan / IReadOnlyCollection source overloads (Func mapper)
    // ============================================================

    /// <summary>Converts a T[] to an array using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[]? ToArray<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var arr = new TDest[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            arr[i] = mapper(src[i]);
        }
        return arr;
    }

    /// <summary>Converts a List&lt;T&gt; to an array using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[]? ToArray<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var arr = new TDest[src.Length];
        for (var i = 0; i < src.Length; i++)
        {
            arr[i] = mapper(src[i]);
        }
        return arr;
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to an array using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[] ToArray<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var arr = new TDest[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            arr[i] = mapper(source[i]);
        }
        return arr;
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to an array using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TDest[]? ToArray<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var arr = new TDest[source.Count];
        var i = 0;
        foreach (var item in source)
        {
            arr[i++] = mapper(item);
        }
        return arr;
    }

    /// <summary>Converts a T[] to a List using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest>? ToList<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var list = new List<TDest>(src.Length);
        CollectionsMarshal.SetCount(list, src.Length);
        var dst = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < src.Length; i++)
        {
            dst[i] = mapper(src[i]);
        }
        return list;
    }

    /// <summary>Converts a List&lt;T&gt; to a List using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest>? ToList<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var list = new List<TDest>(src.Length);
        CollectionsMarshal.SetCount(list, src.Length);
        var dst = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < src.Length; i++)
        {
            dst[i] = mapper(src[i]);
        }
        return list;
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to a List using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest> ToList<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var list = new List<TDest>(source.Length);
        CollectionsMarshal.SetCount(list, source.Length);
        var dst = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < source.Length; i++)
        {
            dst[i] = mapper(source[i]);
        }
        return list;
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to a List using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<TDest>? ToList<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var list = new List<TDest>(source.Count);
        foreach (var item in source)
        {
            list.Add(mapper(item));
        }
        return list;
    }

    /// <summary>Converts a T[] to an ImmutableArray using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return [];
        }

        var src = source.AsSpan();
        var builder = ImmutableArray.CreateBuilder<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.MoveToImmutable();
    }

    /// <summary>Converts a List&lt;T&gt; to an ImmutableArray using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return [];
        }

        var src = CollectionsMarshal.AsSpan(source);
        var builder = ImmutableArray.CreateBuilder<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.MoveToImmutable();
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to an ImmutableArray using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var builder = ImmutableArray.CreateBuilder<TDest>(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            builder.Add(mapper(source[i]));
        }
        return builder.MoveToImmutable();
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to an ImmutableArray using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<TDest>(source.Count);
        foreach (var item in source)
        {
            builder.Add(mapper(item));
        }
        return builder.MoveToImmutable();
    }

    /// <summary>Converts a T[] to an ImmutableList using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableList<TDest>? ToImmutableList<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var builder = ImmutableList.CreateBuilder<TDest>();
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a List&lt;T&gt; to an ImmutableList using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableList<TDest>? ToImmutableList<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var builder = ImmutableList.CreateBuilder<TDest>();
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to an ImmutableList using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableList<TDest> ToImmutableList<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var builder = ImmutableList.CreateBuilder<TDest>();
        for (var i = 0; i < source.Length; i++)
        {
            builder.Add(mapper(source[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to an ImmutableList using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableList<TDest>? ToImmutableList<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var builder = ImmutableList.CreateBuilder<TDest>();
        foreach (var item in source)
        {
            builder.Add(mapper(item));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a T[] to a HashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TDest>? ToHashSet<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var set = new HashSet<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            set.Add(mapper(src[i]));
        }
        return set;
    }

    /// <summary>Converts a List&lt;T&gt; to a HashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TDest>? ToHashSet<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var set = new HashSet<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            set.Add(mapper(src[i]));
        }
        return set;
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to a HashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TDest> ToHashSet<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var set = new HashSet<TDest>(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            set.Add(mapper(source[i]));
        }
        return set;
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to a HashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TDest>? ToHashSet<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var set = new HashSet<TDest>(source.Count);
        foreach (var item in source)
        {
            set.Add(mapper(item));
        }
        return set;
    }

    /// <summary>Converts a T[] to an ImmutableHashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<TDest>? ToImmutableHashSet<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var builder = ImmutableHashSet.CreateBuilder<TDest>();
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a List&lt;T&gt; to an ImmutableHashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<TDest>? ToImmutableHashSet<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var builder = ImmutableHashSet.CreateBuilder<TDest>();
        for (var i = 0; i < src.Length; i++)
        {
            builder.Add(mapper(src[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to an ImmutableHashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<TDest> ToImmutableHashSet<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var builder = ImmutableHashSet.CreateBuilder<TDest>();
        for (var i = 0; i < source.Length; i++)
        {
            builder.Add(mapper(source[i]));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to an ImmutableHashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<TDest>? ToImmutableHashSet<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var builder = ImmutableHashSet.CreateBuilder<TDest>();
        foreach (var item in source)
        {
            builder.Add(mapper(item));
        }
        return builder.ToImmutable();
    }

    /// <summary>Converts a T[] to a FrozenSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<TDest>? ToFrozenSet<TSource, TDest>(
        TSource[]? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = source.AsSpan();
        var set = new HashSet<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            set.Add(mapper(src[i]));
        }
        return set.ToFrozenSet();
    }

    /// <summary>Converts a List&lt;T&gt; to a FrozenSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<TDest>? ToFrozenSet<TSource, TDest>(
        List<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var src = CollectionsMarshal.AsSpan(source);
        var set = new HashSet<TDest>(src.Length);
        for (var i = 0; i < src.Length; i++)
        {
            set.Add(mapper(src[i]));
        }
        return set.ToFrozenSet();
    }

    /// <summary>Converts a ReadOnlySpan&lt;T&gt; to a FrozenSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<TDest> ToFrozenSet<TSource, TDest>(
        ReadOnlySpan<TSource> source,
        Func<TSource, TDest> mapper)
    {
        var set = new HashSet<TDest>(source.Length);
        for (var i = 0; i < source.Length; i++)
        {
            set.Add(mapper(source[i]));
        }
        return set.ToFrozenSet();
    }

    /// <summary>Converts an IReadOnlyCollection&lt;T&gt; to a FrozenSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<TDest>? ToFrozenSet<TSource, TDest>(
        IReadOnlyCollection<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        var set = new HashSet<TDest>(source.Count);
        foreach (var item in source)
        {
            set.Add(mapper(item));
        }
        return set.ToFrozenSet();
    }

    // ============================================================
    // Immutable / Frozen / HashSet collections (Func mapper)
    // ============================================================

    /// <summary>Converts a collection to an ImmutableArray using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return [];
        }

        return source.Select(mapper).ToImmutableArray();
    }

    /// <summary>Converts a collection to an ImmutableList using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableList<TDest>? ToImmutableList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToImmutableList();
    }

    /// <summary>Converts a collection to a HashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSet<TDest>? ToHashSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToHashSet();
    }

    /// <summary>Converts a collection to an ImmutableHashSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImmutableHashSet<TDest>? ToImmutableHashSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToImmutableHashSet();
    }

    /// <summary>Converts a collection to a FrozenSet using the specified mapper function.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FrozenSet<TDest>? ToFrozenSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Func<TSource, TDest> mapper)
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(mapper).ToFrozenSet();
    }

    // ============================================================
    // Immutable / Frozen / HashSet collections (Action mapper)
    // ============================================================

    /// <summary>Converts a collection to an ImmutableArray using the specified void mapper action.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return [];
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToImmutableArray();
    }

    /// <summary>Converts a collection to an ImmutableList using the specified void mapper action.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static ImmutableList<TDest>? ToImmutableList<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToImmutableList();
    }

    /// <summary>Converts a collection to a HashSet using the specified void mapper action.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static HashSet<TDest>? ToHashSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToHashSet();
    }

    /// <summary>Converts a collection to an ImmutableHashSet using the specified void mapper action.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static ImmutableHashSet<TDest>? ToImmutableHashSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToImmutableHashSet();
    }

    /// <summary>Converts a collection to a FrozenSet using the specified void mapper action.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [RequiresUnreferencedCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    [RequiresDynamicCode("Action-based overloads use new() constraint which may not be AOT-safe. Use generated mapper code instead.")]
    public static FrozenSet<TDest>? ToFrozenSet<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return default;
        }

        return source.Select(x =>
        {
            var dest = new TDest();
            mapper(x, dest);
            return dest;
        }).ToFrozenSet();
    }
}
