namespace Smart.Mapper.Generator.Models;

// Represents a custom parameter passed to a mapper method.
internal sealed record CustomParameterModel
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}
