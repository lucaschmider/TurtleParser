using CommandLine;

namespace TurtleParser;

public class CommandLineArgs
{
    [Option('o', "output", Required = true, HelpText = "Override the output file")]
    public string OutputFile { get; set; }

    [Option('i', "input", Required = true, HelpText = "Specifies the location of the input file")]
    public string InputFile { get; set; }

    [Option('q', "qualify", Required = false,
        HelpText = "Specifies whether to replace known prefixes with their fully qualified URIs", Default = false)]
    public bool FullyQualify { get; set; }

    [Option('g', "generator", Required = true,
        HelpText = "Specifies which generator should be used. Could be either 'Graph' or 'Prolog'")]
    public GeneratorType Generator { get; set; }

    [Option('r', "reader", Required = false, Hidden = true, Default = ReaderType.Turtle,
        HelpText =
            "Specifies which reader to use. Currently only the turtle reader is implemented, so the option can be ommited")]
    public ReaderType Reader { get; set; }
}

public enum GeneratorType
{
    Graph,
    Prolog
}

public enum ReaderType
{
    Turtle
}