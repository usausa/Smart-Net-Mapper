namespace Smart.Mapper.Generator.Models;

/// <summary>
/// Represents a custom parameter passed to a mapper method.
/// </summary>
internal sealed record CustomParameterModel
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}
