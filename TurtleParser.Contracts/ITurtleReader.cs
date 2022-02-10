namespace TurtleParser.Contracts;

public interface ITurtleReader
{
    IEnumerable<Triple> GetTriples(bool fullyQualify);
}