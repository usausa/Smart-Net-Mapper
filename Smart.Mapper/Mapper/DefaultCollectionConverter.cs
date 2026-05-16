namespace Smart.Mapper;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

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
            return ImmutableArray<TDest>.Empty;
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
    public static ImmutableArray<TDest> ToImmutableArray<TSource, TDest>(
        IEnumerable<TSource>? source,
        Action<TSource, TDest> mapper)
        where TDest : new()
    {
        if (source is null)
        {
            return ImmutableArray<TDest>.Empty;
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
