namespace Smart.Mapper.Models;

// MapExpression: several expressions in one mapper declaring the same variable names (out var n,
// is int n). Each expression is compiled as a static local function, so the names do not collide.
public class ExpressionSource
{
    public string? First { get; set; }
    public string? Second { get; set; }
    public object? Boxed { get; set; }
    public object? OtherBoxed { get; set; }
}

public class ExpressionDestination
{
    public int First { get; set; }
    public int Second { get; set; }
    public int Boxed { get; set; }
    public int OtherBoxed { get; set; }
    public string? Label { get; set; }
}

// init-only / required targets are assigned in the object initializer
public class ExpressionInitDestination
{
    public required int First { get; set; }
    public int Second { get; init; }
    public int Boxed { get; init; }
}

// Custom parameter used inside expressions; Next records the evaluation order
public class ExpressionContext
{
    private int count;

    public int Offset { get; set; }

    public int Next() => ++count;
}
