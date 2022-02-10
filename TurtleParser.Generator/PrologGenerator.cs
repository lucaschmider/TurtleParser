using System.Text.RegularExpressions;
using TurtleParser.Contracts;

namespace TurtleParser.Generator;

public class PrologGenerator : IGenerator
{
    /// <summary>
    ///     Sanitizes all identifiers to prolog friendly names and writes the tuples to the output file.
    /// </summary>
    /// <param name="triples"></param>
    /// <param name="outputPath"></param>
    /// <returns></returns>
    public Task GenerateOutputAsync(IEnumerable<Triple> triples, string outputPath)
    {
        var lines = triples
            .Select(triple => new Triple(SanitizeIdentifier(triple.Subject),
                SanitizeIdentifier(triple.Predicate),
                SanitizeIdentifier(triple.Object)))
            .Select(sanitizedTriple =>
                $"{sanitizedTriple.Predicate}({sanitizedTriple.Subject}, {sanitizedTriple.Object}).");
        var file = string.Join('\n', lines);

        return File.WriteAllTextAsync(outputPath, file);
    }

    private static string SanitizeIdentifier(string initialIdentifier)
    {
        var uriPattern = new Regex(@"<.+>");
        var identifier = initialIdentifier;

        if (uriPattern.IsMatch(initialIdentifier))
        {
            var uri = new Uri(initialIdentifier);
            identifier = uri.GetComponents(UriComponents.Host | UriComponents.PathAndQuery | UriComponents.Fragment,
                UriFormat.Unescaped);
        }

        var nextCharShouldBeUppercase = false;
        var allowedCharPattern = new Regex(@"\w");
        return identifier
            .Select(c =>
            {
                var charString = c.ToString();
                if (allowedCharPattern.IsMatch(charString))
                {
                    if (!nextCharShouldBeUppercase) return charString;

                    nextCharShouldBeUppercase = false;
                    return charString.ToUpperInvariant();
                }

                nextCharShouldBeUppercase = true;
                return string.Empty;
            })
            .Aggregate(string.Empty, (accumulator, c) => accumulator + c)
            .FirstCharToLowerCase();
    }
}