using System.CommandLine;
using System.Xml.Linq;

namespace ResxSorter;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        CliConfiguration configuration = GetConfiguration();
        return configuration.InvokeAsync(args);
    }

    public static CliConfiguration GetConfiguration()
    {
        CliOption<FileInfo> inputFileOption = new("--input-file", "-i")
        {
            Description = "The input resx file",
            Required = true,
        };
        inputFileOption.AcceptExistingOnly();
        CliOption<FileInfo> outputFileOption = new("--output-file", "-o")
        {
            Description = "The output file."
        };
        CliOption<bool> forceOption = new("--force", "-f")
        {
            Description = "Always write the output, even if there is no change."
        };
        CliOption<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "Write verbose output"
        };

        CliRootCommand rootCommand = new("Sorts elements in a resx file")
        {
            new CliCommand("no-op"),
            inputFileOption,
            outputFileOption,
            forceOption
        };

        rootCommand.SetAction((ParseResult parseResult) =>
        {
            FileInfo inputFile = parseResult.CommandResult.GetValue(inputFileOption)!;
            FileInfo? outputFile = parseResult.CommandResult.GetValue(outputFileOption);
            bool force = parseResult.CommandResult.GetValue(forceOption);
            bool verbose = parseResult.CommandResult.GetValue(verboseOption);

            if (verbose)
            {
                Console.WriteLine($"Input {inputFile}, Output: {outputFile}, Force: {force}");
            }

            bool writeOutputFile = force;
            if (!writeOutputFile)
            {
                using Stream inputStream = inputFile.OpenRead();
                XDocument document = XDocument.Load(inputStream);
                //Check if the elements are ordered
                string lastName = "";
                foreach (var element in document.Root!.Elements(XName.Get("data")))
                {
                    string resourceName = element.Attributes().Single(x => x.Name.LocalName == "name").Value;
                    if (string.Compare(lastName, resourceName) > 0)
                    {
                        writeOutputFile = true;
                        break;
                    }
                    lastName = resourceName;
                }
                if (verbose)
                {
                    if (writeOutputFile)
                    {
                        Console.WriteLine($"Input {inputFile} is not sorted");
                    }
                    else
                    {
                        Console.WriteLine($"Input {inputFile} is already sorted");
                    }
                }
            }
            if (writeOutputFile || outputFile?.Exists == false)
            {
                using MemoryStream ms = new();
                using (Stream inputStream = inputFile.OpenRead())
                {
                    XDocument document = XDocument.Load(inputStream);

                    var elements = document.Root!.Elements(XName.Get("data"))
                        .OrderBy(x => x.Attributes().Single(x => x.Name.LocalName == "name").Value)
                        .ToList();
                    foreach (var element in elements)
                    {
                        element.Remove();
                    }
                    foreach (var element in elements)
                    {
                        document.Root.Add(element);
                    }
                    document.Save(ms);
                }
                ms.Position = 0;
                if (outputFile != null)
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Writing sorted file to {outputFile}");
                    }
                    outputFile.Directory?.Create();
                    using Stream outputStream = outputFile.OpenWrite();
                    ms.CopyTo(outputStream);
                }
                else
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Overwriting input file with sorted data {inputFile}");
                    }
                    using Stream outputStream = inputFile.OpenWrite();
                    ms.CopyTo(outputStream);
                }
            }

        });
        return new CliConfiguration(rootCommand);
    }
}