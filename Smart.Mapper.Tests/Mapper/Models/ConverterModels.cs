namespace Smart.Mapper.Models;

public class ConverterSource
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class ConverterDestination
{
    public string ConvertedValue { get; set; } = string.Empty;
    public string FormattedText { get; set; } = string.Empty;
}

public class CustomConverterSource
{
    public int IntValue { get; set; }
    public string StringValue { get; set; } = string.Empty;
}

public class CustomConverterDestination
{
    public string IntValue { get; set; } = string.Empty;
    public int StringValue { get; set; }
}

public static class TestCustomConverter
{
    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        if (typeof(TSource) == typeof(int) && typeof(TDestination) == typeof(string))
        {
            var value = (int)(object)source!;
            return (TDestination)(object)$"PREFIX_{value}";
        }

        if (typeof(TSource) == typeof(string) && typeof(TDestination) == typeof(int))
        {
            var value = (string)(object)source!;
            if (value.StartsWith("NUM_", StringComparison.Ordinal))
            {
                return (TDestination)(object)int.Parse(value[4..], System.Globalization.CultureInfo.InvariantCulture);
            }
            return (TDestination)(object)int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        return DefaultValueConverter.Convert<TSource, TDestination>(source);
    }
}

public class SpecializedConverterSource
{
    public string StringValue { get; set; } = string.Empty;
    public int IntValue { get; set; }
    public double DoubleValue { get; set; }
}

public class SpecializedConverterDestination
{
    public int StringValue { get; set; }
    public string IntValue { get; set; } = string.Empty;
    public decimal DoubleValue { get; set; }
}

public static class SpecializedConverter
{
    public static int ConvertToInt32(string source)
    {
        return int.Parse(source, System.Globalization.CultureInfo.InvariantCulture) + 1000;
    }

    public static string ConvertToString(int source)
    {
        return $"SPEC_{source}";
    }

    public static TDestination Convert<TSource, TDestination>(TSource source)
    {
        return DefaultValueConverter.Convert<TSource, TDestination>(source);
    }
}
