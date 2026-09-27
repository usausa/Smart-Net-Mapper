namespace Smart.Mapper.Models;

// Types declared with nullable annotations disabled, whose references may hold null all the same, so NullValue and
// NullBehavior.Skip apply to them
#nullable disable
public class ObliviousSource
{
    public string Name { get; set; }

    public string Note { get; set; }
}

public class ObliviousDestination
{
    public string Name { get; set; }

    public string Note { get; set; }
}
#nullable restore
