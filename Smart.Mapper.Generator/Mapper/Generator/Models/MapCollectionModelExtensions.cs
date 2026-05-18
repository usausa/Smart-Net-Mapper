namespace Smart.Mapper.Generator.Models;

internal static class MapCollectionModelExtensions
{
    public static bool HasCustomConverter(this MapCollectionModel m) => !string.IsNullOrEmpty(m.Converter);
}
