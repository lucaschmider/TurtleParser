using CommandLine;
using TurtleParser;
using TurtleParser.Contracts;
using TurtleParser.Generator;
using TurtleParser.Reader;

var readers = new Dictionary<ReaderType, Func<Stream, IReader>>
{
    {ReaderType.Turtle, stream => new TurtleReader(stream)}
};
var generators = new Dictionary<GeneratorType, Func<IGenerator>>
{
    {GeneratorType.Graph, () => new GraphGenerator()},
    {GeneratorType.Prolog, () => new PrologGenerator()}
};

new Parser(with =>
    {
        with.CaseInsensitiveEnumValues = true;
        with.HelpWriter = Console.Out;
    })
    .ParseArguments<CommandLineArgs>(args)
    .WithParsedAsync(async options =>
    {
        var charStream = File.OpenRead(options.InputFile);


        if (!readers.ContainsKey(options.Reader))
        {
            Console.WriteLine("[ERROR] The specified reader is not implemented!");
            return;
        }

        if (!generators.ContainsKey(options.Generator))
        {
            Console.WriteLine("[ERROR] The specified generator is not implemented!");
            return;
        }

        var reader = readers[options.Reader].Invoke(charStream);
        var outputGenerator = generators[options.Generator].Invoke();

        var triples = reader.GetTriples(options.FullyQualify);
        await outputGenerator
            .GenerateOutputAsync(triples, options.OutputFile)
            .ConfigureAwait(true);
    });