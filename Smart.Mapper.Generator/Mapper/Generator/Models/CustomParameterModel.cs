namespace Smart.Mapper.Generator.Models;

// Represents a custom parameter passed to a mapper method.
internal sealed record CustomParameterModel
{
    public string Name { get; set; } = default!;
    public string TypeName { get; set; } = default!;
}
