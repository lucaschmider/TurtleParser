using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TurtleParser.Contracts;

namespace TurtleParser.Generator;

/// <summary>
///     Implements the conversion of triples into a visual representation.
/// </summary>
public class GraphGenerator : IGenerator
{
    private const string HtmlTemplate =
        "<!DOCTYPE html>\n<html lang=\"en\">\n  <head>\n    <meta charset=\"UTF-8\" />\n    <meta content=\"IE=edge\" http-equiv=\"X-UA-Compatible\" />\n    <meta content=\"width=device-width, initial-scale=1.0\" name=\"viewport\" />\n    <title>Document</title>\n    <style>\n      #cy {\n        width: 100vw;\n        height: 100vh;\n        display: block;\n      }\n    </style>\n    <script src=\"https://pagecdn.io/lib/cytoscape/3.20.0/cytoscape.min.js\"></script>\n  </head>\n  <body>\n    <div id=\"cy\"></div>\n\n    <script>\n         const elements = ##DATA##;\n      var cy = cytoscape({\n        container: document.getElementById(\"cy\"), // container to render in\n        elements,\n        style: [\n          {\n            selector: \"node\",\n            style: {\n              \"background-color\": \"#666\",\n              label: \"data(id)\",\n            },\n          },\n          {\n            selector: \"edge\",\n            style: {\n              width: 3,\n              \"line-color\": \"#ccc\",\n              \"target-arrow-color\": \"#ccc\",\n              \"target-arrow-shape\": \"triangle\",\n              \"curve-style\": \"bezier\",\n              label: \"data(name)\",\n            },\n          },\n        ],\n      });\n\n      var layout = cy.elements().layout({\n        name: \"circle\",\n      });\n\n      layout.run();\n    </script>\n  </body>\n</html>\n";

    /// <summary>
    ///     Initiates the conversion of triples
    /// </summary>
    /// <param name="triples"></param>
    /// <param name="outputPath"></param>
    /// <returns></returns>
    public Task GenerateOutputAsync(IEnumerable<Triple> triples, string outputPath)
    {
        var triplesArray = triples.ToArray();
        Console.WriteLine($"Got {triplesArray.Length} triples to visualize");
        var serializedData = CreateJsonData(triplesArray);
        var path = Path.GetFullPath(outputPath);
        Console.WriteLine($"Writing output to file: {path}");

        return SaveGeneratedFile(serializedData, path);
    }

    /// <summary>
    ///     Maps the provided triples into an array of data points an serializes them into a JSON string that can be injected
    ///     into the html template.
    /// </summary>
    /// <param name="triples"></param>
    /// <returns></returns>
    private string CreateJsonData(IEnumerable<Triple> triples)
    {
        var tripleArray = triples.ToArray();

        var nodePoints = tripleArray
            .SelectMany(t => new[] {t.Subject, t.Object})
            .Distinct()
            .Select(v => new Element(new DataPoint(v, null, null, null)));
        var edgePoints = tripleArray
            .Select((triple, index) =>
                new Element(new DataPoint($"joint-{index}", triple.Subject, triple.Object, triple.Predicate)));

        var dataPoints = nodePoints.Union(edgePoints);
        var serializedData = JsonConvert.SerializeObject(dataPoints, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        });
        return serializedData;
    }

    /// <summary>
    ///     Injects serialized json data into the embedded html template and saves the output to the specified location
    /// </summary>
    /// <param name="jsonData"></param>
    /// <param name="path"></param>
    private async Task SaveGeneratedFile(string jsonData, string path)
    {
        var generatedFile = HtmlTemplate.Replace("##DATA##", jsonData);

        Console.WriteLine(new string('-', Console.WindowWidth));
        Console.WriteLine(generatedFile);
        Console.WriteLine(new string('-', Console.WindowWidth));

        await File
            .WriteAllTextAsync(path, generatedFile)
            .ConfigureAwait(false);
    }

    /// <summary>
    ///     The visualization framework requires that data is provided in a field called 'data'. This wrapper class reflects
    ///     that requirement.
    /// </summary>
    /// <param name="Data"></param>
    // ReSharper disable NotAccessedPositionalProperty.Local
    private record Element(DataPoint Data);

    /// <summary>
    ///     Represents a data point, which could either be a node or an edge. Which one is determined by the existence of the
    ///     source and target fields.
    /// </summary>
    /// <param name="Id">
    ///     The visualization framework requires data points to be uniquely identifiable. In case of a node, this
    ///     field also represents the display value.
    /// </param>
    /// <param name="Source">
    ///     In case the data point described by the current instance is an edge, this field specifies at which
    ///     node it should start.
    /// </param>
    /// <param name="Target">
    ///     In case the data point described by the current instance is an edge, this field specifies at which
    ///     node it should end.
    /// </param>
    /// <param name="Name">
    ///     In case the data point described by the current instance is an edge, this field specifies the label
    ///     of it.
    /// </param>
    private record DataPoint(string Id, string? Source, string? Target, string? Name);
    // ReSharper restore NotAccessedPositionalProperty.Local
}