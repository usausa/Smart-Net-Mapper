namespace Smart.Mapper;

/// <summary>
/// Specifies the behavior when the source value is null.
/// </summary>
public enum NullBehavior
{
    /// <summary>
    /// Sets default! to the target property when the source is null.
    /// This is the default behavior.
    /// </summary>
    SetDefault = 0,

    /// <summary>
    /// Skips setting the target property when the source is null.
    /// </summary>
    Skip = 1
}
