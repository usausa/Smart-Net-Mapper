namespace Smart.Mapper;

using System;

/// <summary>
/// Specifies that this method is a mapper method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MapperAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether to automatically map properties with matching names.
    /// Default is true.
    /// </summary>
    public bool AutoMap { get; set; } = true;
}
