using Smart.Mapper.AotTests;

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        Console.Error.WriteLine($"FAIL: {message}");
        Environment.Exit(1);
    }
}

Console.WriteLine("Smart.Mapper AOT smoke tests starting...");

// 1. Basic void mapping
{
    var src = new BasicSource { Id = 1, Name = "Alice" };
    var dst = new BasicDest();
    AotMappers.Map(src, dst);
    Assert(dst.Id == 1, "Basic void Map: Id");
    Assert(dst.Name == "Alice", "Basic void Map: Name");
    Console.WriteLine("  [OK] Basic void mapping");
}

// 2. Basic return mapping
{
    var src = new BasicSource { Id = 2, Name = "Bob" };
    var dst = AotMappers.MapToNew(src);
    Assert(dst.Id == 2, "Basic MapToNew: Id");
    Assert(dst.Name == "Bob", "Basic MapToNew: Name");
    Console.WriteLine("  [OK] Basic return mapping");
}

// 3. Type conversion
{
    var src = new TypeConvSource { IntVal = 42, LongVal = 99L, DoubleVal = 3.14 };
    var dst = new TypeConvDest();
    AotMappers.Map(src, dst);
    Assert(dst.IntVal == "42", "TypeConv: IntVal->string");
    Assert(dst.LongVal == 99, "TypeConv: LongVal->int");
    Console.WriteLine("  [OK] Type conversion");
}

// 4. Enum mapping
{
    var src = new EnumSource { Status = SrcStatus.Inactive };
    var dst = new EnumDest();
    AotMappers.Map(src, dst);
    Assert(dst.Status == DstStatus.Inactive, "Enum: Status");
    Console.WriteLine("  [OK] Enum mapping");
}

// 5. Null handling
{
    var src = new NullSource { Name = null, Count = null };
    var dst = new NullDest();
    AotMappers.Map(src, dst);
    Assert(dst.Name == "", "NullSubstitute: Name");
    Assert(dst.Count == 0, "NullSubstitute: Count");
    Console.WriteLine("  [OK] Null handling");
}

// 6. Nested property
{
    var src = new FlatSrc { Child = new ChildSrc { Value = 77 }, DirectVal = 5 };
    var dst = new FlatDst();
    AotMappers.Map(src, dst);
    Assert(dst.ChildValue == 77, "Nested: ChildValue");
    Assert(dst.DirectVal == 5, "Nested: DirectVal");
    Console.WriteLine("  [OK] Nested property mapping");
}

// 7. Collection mapping
{
    var src = new CollSrc
    {
        Items = [new ItemSrc { Id = 1, Label = "x" }, new ItemSrc { Id = 2, Label = "y" }]
    };
    var dst = new CollDst();
    AotMappers.Map(src, dst);
    Assert(dst.Items?.Count == 2, "Collection: Items count");
    Assert(dst.Items![0].Id == 1, "Collection: Items[0].Id");
    Assert(dst.Items![1].Label == "y", "Collection: Items[1].Label");
    Console.WriteLine("  [OK] Collection mapping");
}

// 8. Custom value converter
{
    var src = new CvtSource { Value = 123 };
    var dst = new CvtDest();
    AotMappers.Map(src, dst);
    Assert(dst.Value == "123", "CustomConverter: Value");
    Console.WriteLine("  [OK] Custom value converter");
}

Console.WriteLine("All AOT smoke tests passed.");
