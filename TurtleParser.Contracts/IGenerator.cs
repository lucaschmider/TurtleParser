namespace TurtleParser.Contracts;

public interface IGenerator
{
    Task GenerateOutputAsync(IEnumerable<Triple> triples, string outputPath);
}