namespace Smart.Mapper;

using Smart.Mapper.Functions;

using Xunit;

public class ConverterTest
{
    //--------------------------------------------------------------------------------
    // Converter
    //--------------------------------------------------------------------------------

    [Fact]
    public void UseConverter()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<Source, BoolDestination>(new Source { Value = -1 }).Value);
    }

    [Fact]
    public void UseConverterUnderlyingSourceToDestination()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableSource, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.False(mapper.Map<NullableSource, BoolDestination>(new NullableSource { Value = null }).Value);
        Assert.True(mapper.Map<NullableSource, BoolDestination>(new NullableSource { Value = -1 }).Value);
    }

    [Fact]
    public void UseConverterSourceToUnderlyingDestination()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, NullableBoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<Source, NullableBoolDestination>(new Source { Value = -1 }).Value);
    }

    [Fact]
    public void UseConverterUnderlyingSourceToUnderlyingDestination()
    {
        var config = new MapperConfig();
        config.CreateMap<NullableSource, NullableBoolDestination>();
        using var mapper = config.ToMapper();

        Assert.Null(mapper.Map<NullableSource, NullableBoolDestination>(new NullableSource { Value = null }).Value);
        Assert.True(mapper.Map<NullableSource, NullableBoolDestination>(new NullableSource { Value = -1 }).Value);
    }

    [Fact]
    public void UseConverterNullableSourceToDestination()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue?, bool>(x => x!.Value.RawValue != 0));
        config.CreateMap<StructValueSource, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<StructValueSource, BoolDestination>(new StructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterNullableSourceToUnderlyingDestination()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue?, bool>(x => x!.Value.RawValue != 0));
        config.CreateMap<StructValueSource, NullableBoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<StructValueSource, NullableBoolDestination>(new StructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterNullableSourceToUnderlyingDestination2()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue?, bool>(x => x!.Value.RawValue != 0));
        config.CreateMap<NullableStructValueSource, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<NullableStructValueSource, BoolDestination>(new NullableStructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
    }

    //--------------------------------------------------------------------------------
    // Class/Struct
    //--------------------------------------------------------------------------------

    [Fact]
    public void UseConverterForClass()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<ClassValue, bool>(x => x.RawValue != 0));
        config.CreateMap<ClassValueSource, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<ClassValueSource, BoolDestination>(new ClassValueSource { Value = new ClassValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterForClassToNullable()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<ClassValue, bool>(x => x.RawValue != 0));
        config.CreateMap<ClassValueSource, NullableBoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<ClassValueSource, NullableBoolDestination>(new ClassValueSource { Value = new ClassValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterForClassWithNullIf()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<ClassValue, bool>(x => x.RawValue != 0));
        config.CreateMap<ClassValueSource, BoolDestination>()
            .ForMember(x => x.Value, opt => opt.NullIf(true));
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<ClassValueSource, BoolDestination>(new ClassValueSource { Value = new ClassValue { RawValue = -1 } }).Value);
        Assert.True(mapper.Map<ClassValueSource, BoolDestination>(new ClassValueSource { Value = null }).Value);
    }

    [Fact]
    public void UseConverterForNullable()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue, bool>(x => x.RawValue != 0));
        config.CreateMap<NullableStructValueSource, BoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<NullableStructValueSource, BoolDestination>(new NullableStructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterForNullableToNullable()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue, bool>(x => x.RawValue != 0));
        config.CreateMap<NullableStructValueSource, NullableBoolDestination>();
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<NullableStructValueSource, NullableBoolDestination>(new NullableStructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
    }

    [Fact]
    public void UseConverterForNullableWithNullIf()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<StructValue, bool>(x => x.RawValue != 0));
        config.CreateMap<NullableStructValueSource, NullableBoolDestination>()
            .ForMember(x => x.Value, opt => opt.NullIf(true));
        using var mapper = config.ToMapper();

        Assert.True(mapper.Map<NullableStructValueSource, NullableBoolDestination>(new NullableStructValueSource { Value = new StructValue { RawValue = -1 } }).Value);
        Assert.True(mapper.Map<NullableStructValueSource, NullableBoolDestination>(new NullableStructValueSource { Value = null }).Value);
    }

    //--------------------------------------------------------------------------------
    // Default option
    //--------------------------------------------------------------------------------

    [Fact]
    public void UseConverterByDefaultFunc()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<int, string>(x => $"#{x}"));
        config.CreateMap<Source, StringDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }).Value);
    }

    [Fact]
    public void UseConverterByDefaultFuncContext()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<int, string>((x, c) => $"{c.Parameter}{x}"));
        config.CreateMap<Source, StringDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    [Fact]
    public void UseConverterByDefaultConverter()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing<int, string, CustomValueConverter>());
        config.CreateMap<Source, StringDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    [Fact]
    public void UseConverterByDefaultConverter2()
    {
        var config = new MapperConfig();
        config.Default(opt => opt.ConvertUsing(new CustomValueConverter()));
        config.CreateMap<Source, StringDestination>();
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    //--------------------------------------------------------------------------------
    // Mapping option
    //--------------------------------------------------------------------------------

    [Fact]
    public void UseConverterByMappingFunc()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, StringDestination>()
            .Default(opt => opt.ConvertUsing<int, string>(x => $"#{x}"));
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }).Value);
    }

    [Fact]
    public void UseConverterByMappingFuncContext()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, StringDestination>()
            .Default(opt => opt.ConvertUsing<int, string>((x, c) => $"{c.Parameter}{x}"));
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    [Fact]
    public void UseConverterByMappingConverter()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, StringDestination>()
            .Default(opt => opt.ConvertUsing<int, string, CustomValueConverter>());
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    [Fact]
    public void UseConverterByMappingConverter2()
    {
        var config = new MapperConfig();
        config.CreateMap<Source, StringDestination>()
            .Default(opt => opt.ConvertUsing(new CustomValueConverter()));
        using var mapper = config.ToMapper();

        Assert.Equal("#1", mapper.Map<Source, StringDestination>(new Source { Value = 1 }, "#").Value);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public class Source
    {
        public int Value { get; set; }
    }

    public class NullableSource
    {
        public int? Value { get; set; }
    }

    public struct StructValue
    {
        public int RawValue { get; set; }
    }

    public class StructValueSource
    {
        public StructValue Value { get; set; }
    }

    public class NullableStructValueSource
    {
        public StructValue? Value { get; set; }
    }

    public class ClassValue
    {
        public int RawValue { get; set; }
    }

    public class ClassValueSource
    {
        public ClassValue? Value { get; set; }
    }

    public class StringDestination
    {
        public string? Value { get; set; }
    }

    public class BoolDestination
    {
        public bool Value { get; set; }
    }

    public class NullableBoolDestination
    {
        public bool? Value { get; set; }
    }

    private sealed class CustomValueConverter : IValueConverter<int, string>
    {
        public string Convert(int value, ResolutionContext context) => $"{context.Parameter}{value}";
    }
}
