namespace Smart.Mapper.Generator.Tests;

using System.Linq;

using Microsoft.CodeAnalysis;

// The generated call of the mapper of [MapNested] / [MapCollection] binds among all the static methods of the name, as
// C# binds it. A mapper taking the source by an implicit reference conversion was used even when the call bound to
// another overload: a more specific one returning another type (CS0029), a generic one, one with an optional parameter,
// or one obsolete as an error (CS0619), all errors in the generated code. Such a call is reported as a mapper that does
// not match (SMP0211 / SMP0210), and when the call binds to another mapper matched, that one is used.
public class MapperOverloadBindingTests
{
    private static bool IsGenerated(Diagnostic diagnostic) =>
        diagnostic.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true;

    private static (string Generated, List<string> Problems) Build(string source)
    {
        var problems = GeneratorTestHelper.GetDiagnosticsAll(source)
            .Where(static d => (d.Severity == DiagnosticSeverity.Error) || ((d.Severity == DiagnosticSeverity.Warning) && IsGenerated(d)) || d.Id.StartsWith("SMP", StringComparison.Ordinal))
            .Where(static d => d.Id != "CS8795")
            .Select(static d => d.Id)
            .ToList();
        return (GeneratorTestHelper.GetGeneratedSource(source).Replace("\r\n", "\n", StringComparison.Ordinal), problems);
    }

    private static string Source(string attribute, string methods) =>
        $$"""
        #nullable enable
        using System;
        using System.Collections.Generic;
        using Smart.Mapper;
        namespace Test;
        public interface IAnimal { string Name { get; } }
        public class Animal : IAnimal { public string Name { get; set; } = ""; }
        public sealed class Dog : Animal { }
        public class AnimalDto { public string Name { get; set; } = ""; }
        public sealed class DogDto : AnimalDto { }
        public sealed class DogCard { public string Name { get; set; } = ""; }
        public class Zoo { public Dog Star { get; set; } = new(); public List<Dog> Dogs { get; set; } = new(); }
        public class ZooDto { public AnimalDto Star { get; set; } = new(); public List<AnimalDto> Dogs { get; set; } = new(); }
        public static partial class M
        {
            [Mapper(AutoMap = false)]
            {{attribute}}
            public static partial ZooDto Map(Zoo source);

            {{methods}}
        }
        """;

    // The call binds to another overload the mapper does not match
    [Theory]
    [InlineData("private static DogCard ToDto(Dog d) => new() { Name = d.Name };")]
    [InlineData("[Obsolete(\"\", true)] private static AnimalDto ToDto(Dog d) => new();")]
    [InlineData("private static T ToDto<T>(T d) => d;")]
    [InlineData("private static int ToDto(Dog d, bool deep = false) => 0;")]
    public void CallBindingToAnotherMethodIsReported(string other)
    {
        const string methods = "private static AnimalDto ToDto(Animal a) => new() { Name = a.Name }; ";

        var (_, nestedProblems) = Build(Source("[MapNested(nameof(ZooDto.Star), Mapper = nameof(ToDto))]", methods + other));
        var (_, collectionProblems) = Build(Source("[MapCollection(nameof(ZooDto.Dogs), Mapper = nameof(ToDto))]", methods + other));

        Assert.Equal(["SMP0211"], nestedProblems);
        Assert.Equal(["SMP0210"], collectionProblems);
    }

    // Methods the call does not bind to leave the match as it is: other types, a generic one the argument does not give
    // its type argument, and one of the same type returning a type converting to the target
    [Theory]
    [InlineData("private static DogCard ToDto(DogCard d) => d;")]
    [InlineData("private static List<T> ToDto<T>(IEnumerable<T> items) => new(items);")]
    [InlineData("private static DogDto ToDto(Dog d) => new() { Name = d.Name };")]
    public void MethodTheCallDoesNotBindToDoesNotMatter(string other)
    {
        const string methods = "private static AnimalDto ToDto(Animal a) => new() { Name = a.Name }; ";

        var (nested, nestedProblems) = Build(Source("[MapNested(nameof(ZooDto.Star), Mapper = nameof(ToDto))]", methods + other));
        var (collection, collectionProblems) = Build(Source("[MapCollection(nameof(ZooDto.Dogs), Mapper = nameof(ToDto))]", methods + other));

        Assert.Empty(nestedProblems);
        Assert.Contains("__d.Star = ToDto(source.Star);", nested, StringComparison.Ordinal);
        Assert.Empty(collectionProblems);
        Assert.Contains("= ToDto(__src[__i]);", collection, StringComparison.Ordinal);
    }

    // The call binds to the most specific overload, which the mapper uses when it matches: here the one taking null,
    // whose call has no null check, over the one taking the interface
    [Fact]
    public void MostSpecificMapperIsUsed()
    {
        var (generated, problems) = Build(Source(
            "[MapNested(nameof(ZooDto.Star), Mapper = nameof(ToDto))]",
            "private static AnimalDto ToDto(IAnimal a) => new() { Name = a.Name }; private static DogDto? ToDto(Animal? a) => a is null ? null : new DogDto { Name = a.Name };"));

        Assert.Empty(problems);
        Assert.Contains("__d.Star = ToDto(source.Star)!;", generated, StringComparison.Ordinal);
    }
}
