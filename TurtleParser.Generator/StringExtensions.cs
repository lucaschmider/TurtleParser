namespace TurtleParser.Generator;

public static class StringExtensions
{
    public static string FirstCharToLowerCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var firstChar = char.ToLowerInvariant(input[0]);
        var tail = input.Substring(1);
        return firstChar + tail;
    }
}