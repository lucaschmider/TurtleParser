namespace TurtleParser.Contracts;

/// <summary>
///     Represents a simple "sentence" consisting of a subject, predicate and an object.
/// </summary>
/// <param name="Subject"></param>
/// <param name="Predicate"></param>
/// <param name="Object"></param>
public record Triple(string Subject, string Predicate, string Object);