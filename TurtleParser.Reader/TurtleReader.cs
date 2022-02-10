using System.Text.RegularExpressions;
using TurtleParser.Contracts;

namespace TurtleParser.Reader;

/// <summary>
///     Implements the conversion of a stream of characters into an enumerable object of triples.
/// </summary>
public class TurtleReader : IReader
{
    /// <summary>
    ///     Memoizes any prefixes defined in the turtle source
    /// </summary>
    private readonly Dictionary<string, string> _prefixRegister = new();

    /// <summary>
    ///     Memoizes the parsed triples of the turtle source
    /// </summary>
    private readonly IReadOnlyList<Triple> _triples;

    /// <summary>
    ///     Creates a new <see cref="TurtleReader" /> instance by consuming the specified input stream and parsing the
    ///     information encoded in it.
    /// </summary>
    /// <param name="charStream">A stream of UTF-8 encoded chars representing a turtle file.</param>
    public TurtleReader(Stream charStream)
    {
        var statements = SplitStatements(charStream);
        var triples = new List<Triple>();
        foreach (var statement in statements)
        {
            if (statement.StartsWith("@prefix"))
            {
                ExtractPrefixFromStatement(statement);
                continue;
            }

            var statementTriples = ExtractTriplesFromStatement(statement);
            triples.AddRange(statementTriples);
        }

        _triples = triples.AsReadOnly();
    }

    /// <summary>
    ///     Provides the parsed content of the represented turtle file. If specified, any abbreviated URIs will be fully
    ///     qualified using the prefixes provided by the data source.
    /// </summary>
    /// <param name="fullyQualify">Determines whether to fully qualify the URIs contained in the triples.</param>
    /// <returns></returns>
    public IEnumerable<Triple> GetTriples(bool fullyQualify)
    {
        return fullyQualify
            ? GetFullQualifiedTriples()
            : _triples;
    }

    /// <summary>
    ///     Replaces all abbreviated URIs in any of the triple fields with their fully qualified counter parts.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<Triple> GetFullQualifiedTriples()
    {
        return _triples
            .Select(triple =>
            {
                var subject = GetFullQualifiedUri(triple.Subject);
                var predicate = GetFullQualifiedUri(triple.Predicate);
                var obj = GetFullQualifiedUri(triple.Object);
                return new Triple(subject, predicate, obj);
            });
    }

    /// <summary>
    ///     Ensures that the specified <see cref="identifier" /> is either a literal or a fully qualified URI.
    /// </summary>
    /// <param name="identifier">Either an URI, a Literal or an abbreviated URI.</param>
    /// <returns></returns>
    private string GetFullQualifiedUri(string identifier)
    {
        var prefixesPattern = "(" + string.Join(")|(", _prefixRegister.Keys) + ")";
        var suffixPattern = new Regex($"(?<={prefixesPattern}:).+");

        if (suffixPattern.IsMatch(identifier))
        {
            var parts = identifier.Split(":");
            var prefix = _prefixRegister[parts[0]];
            return prefix + parts[1];
        }

        return identifier.Trim('<', '>');
    }


    /// <summary>
    ///     Interprets the provided <see cref="statement" /> as a source for at least one triple.
    /// </summary>
    /// <param name="statement">The turtle encoded triple(s).</param>
    /// <returns></returns>
    private IEnumerable<Triple> ExtractTriplesFromStatement(string statement)
    {
        var allTokens = SplitIntoTokens(statement)
            .ToArray();

        var subject = allTokens.First();
        var tokensWithoutSubject = allTokens.Skip(1);

        string? currentPredicate = null;
        foreach (var token in tokensWithoutSubject)
        {
            if (currentPredicate == null)
            {
                currentPredicate = token;
                continue;
            }

            yield return new Triple(subject, currentPredicate, token.TrimEnd(';', '.', ','));

            if (token.EndsWith(";") || token.EndsWith(".")) currentPredicate = null;
        }
    }

    /// <summary>
    ///     Splits <see cref="statement" /> by spaces which are not part of URIs or literals.
    /// </summary>
    /// <param name="statement"></param>
    /// <returns></returns>
    private static IEnumerable<string> SplitIntoTokens(string statement)
    {
        var currentToken = string.Empty;
        char? currentBlockDelimiter = null;
        foreach (var nextChar in statement)
        {
            currentToken += nextChar;

            if (nextChar == '<' && currentBlockDelimiter == null)
            {
                currentBlockDelimiter = '>';
            }
            else if (nextChar == '"' && currentBlockDelimiter == null)
            {
                currentBlockDelimiter = '"';
            }
            else if (nextChar == currentBlockDelimiter)
            {
                currentBlockDelimiter = null;
            }
            else if (currentBlockDelimiter == null &&
                     (string.IsNullOrWhiteSpace(nextChar.ToString()) || nextChar == '.'))
            {
                yield return currentToken.Trim();
                currentToken = string.Empty;
            }
        }
    }

    /// <summary>
    ///     Interprets the provided statement as a prefix notation.
    /// </summary>
    /// <param name="statement">The turtle encoded prefix.</param>
    private void ExtractPrefixFromStatement(string statement)
    {
        var identifierPattern = new Regex(@"(?<=@prefix )[\w\d]+(?=:)");
        var identifier = identifierPattern.Match(statement).Value;

        var uriPattern = new Regex(@"(?<=@prefix [\w\d]+:<).+(?=>\.)");
        var uri = uriPattern.Match(statement).Value;
        _prefixRegister.Add(identifier, uri);
    }

    /// <summary>
    ///     Determines whether <see cref="nextChar" /> should be added to <see cref="currentStatement" />. This function
    ///     ensures that no unnecessary whitespace characters are contained in the statement.
    /// </summary>
    /// <param name="nextChar"></param>
    /// <param name="currentStatement"></param>
    /// <param name="isInBlock"></param>
    /// <returns></returns>
    private static string ProcessNextChar(string nextChar, string currentStatement, bool isInBlock)
    {
        var endsWithWhiteSpace = currentStatement.EndsWith(' ');

        var sanitizedNextChar = nextChar switch
        {
            " " when endsWithWhiteSpace && !isInBlock => string.Empty,
            " " => " ",
            _ when string.IsNullOrWhiteSpace(nextChar) => string.Empty,
            _ => nextChar
        };

        return currentStatement + sanitizedNextChar;
    }

    /// <summary>
    ///     Splits the char stream provided via <see cref="inputStream" /> into a list of statements. With that in mind, a
    ///     statement is a range of multiple chars delimited by a dot, which is not contained in a block.
    /// </summary>
    /// <param name="inputStream"></param>
    /// <returns></returns>
    private static IEnumerable<string> SplitStatements(Stream inputStream)
    {
        var streamReader = new StreamReader(inputStream);

        var currentStatement = string.Empty;
        char? currentBlockDelimiter = null;
        while (streamReader.Peek() > -1)
        {
            var nextChar = Convert.ToChar(streamReader.Read());
            currentStatement = ProcessNextChar(nextChar.ToString(), currentStatement, currentBlockDelimiter.HasValue);

            if (nextChar == '<' && currentBlockDelimiter == null)
            {
                currentBlockDelimiter = '>';
            }
            else if (nextChar == '"' && currentBlockDelimiter == null)
            {
                currentBlockDelimiter = '"';
            }
            else if (nextChar == '.' && currentBlockDelimiter == null)
            {
                yield return currentStatement.Trim();
                currentStatement = string.Empty;
            }
            else if (nextChar == currentBlockDelimiter)
            {
                currentBlockDelimiter = null;
            }
        }
    }
}