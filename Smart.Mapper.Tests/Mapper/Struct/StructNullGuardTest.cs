namespace Smart.Mapper.Struct;

public sealed class StructNullGuardTest
{
    //--------------------------------------------------------------------------------
    // Mapper
    //--------------------------------------------------------------------------------

    [Fact]
    public void GuardForAction()
    {
        using var mapper = new MapperConfig()
            .AddDefaultMapper()
            .ToMapper();

        var destination = default(StructDestination);
        mapper.Map<StructSource?, StructDestination>(null!, destination);
    }

    [Fact]
    public void GuardForFunc()
    {
        using var mapper = new MapperConfig()
            .AddDefaultMapper()
            .ToMapper();

        mapper.Map<StructSource?, StructDestination>(null!);
    }

    [Fact]
    public void GuardForParameterAction()
    {
        using var mapper = new MapperConfig()
            .AddDefaultMapper()
            .ToMapper();

        var destination = default(StructDestination);
        mapper.Map<StructSource?, StructDestination>(null!, destination, 0);
    }

    [Fact]
    public void GuardForParameterFunc()
    {
        using var mapper = new MapperConfig()
            .AddDefaultMapper()
            .ToMapper();

        mapper.Map<StructSource?, StructDestination>(null, 0);
    }

    //--------------------------------------------------------------------------------
    // Data
    //--------------------------------------------------------------------------------

    public struct StructSource
    {
    }

    public struct StructDestination
    {
    }
}
