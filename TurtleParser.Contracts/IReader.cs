namespace TurtleParser.Contracts;

public interface IReader
{
    IEnumerable<Triple> GetTriples(bool fullyQualify);
}