namespace Smart.Mapper.Generator.Helpers;

using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis.CSharp;

// Names the generated code writes. A C# keyword used as a name (a member @class, a parameter @in, an enum
// member @int) is written with its @, or it would read as the keyword. Every name the generated code writes,
// of members, parameters, methods, types and namespaces, goes through Escape.
internal static class IdentifierHelper
{
    public static string Escape(string name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    // The name of a type or a type parameter in a type declaration: a contextual keyword gets its @ as well, as
    // a type named after one (record, required, file, scoped) is warned about or refused without it
    public static string EscapeTypeName(string name) =>
        SyntaxFacts.GetContextualKeywordKind(name) == SyntaxKind.None ? Escape(name) : "@" + name;

    // A dotted path of names, such as Child.Value, with each name escaped
    public static string EscapePath(string path) =>
        path.IndexOf('.') < 0 ? Escape(path) : String.Join(".", path.Split('.').Select(Escape));

    // Text made of names and punctuation, such as a namespace or a type name with its type parameters
    // (Mappers<TKey, TValue>), with each name in it escaped
    public static string EscapeNames(string text)
    {
        var result = new StringBuilder(text.Length);
        var start = -1;
        for (var i = 0; i <= text.Length; i++)
        {
            var isNameCharacter = (i < text.Length) && (Char.IsLetterOrDigit(text[i]) || (text[i] == '_'));
            if (isNameCharacter)
            {
                start = start < 0 ? i : start;
                continue;
            }

            if (start >= 0)
            {
                result.Append(Escape(text.Substring(start, i - start)));
                start = -1;
            }

            if (i < text.Length)
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }
}
