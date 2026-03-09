using System.CommandLine;
using System.CommandLine.Parsing;
using System.Xml.Linq;

namespace ResxSorter;

public sealed class Program
{
    private static Task<int> Main(string[] args)
    {
        RootCommand rootCommand = GetRootCommand();
        ParseResult parseResult = rootCommand.Parse(args, new ParserConfiguration());
        return parseResult.InvokeAsync(new InvocationConfiguration());
    }

    public static RootCommand GetRootCommand()
    {
        Option<FileInfo> inputFileOption = new("--input-file", "-i")
        {
            Description = "The input resx file",
            Required = true,
        };
        inputFileOption.AcceptExistingOnly();
        Option<FileInfo> outputFileOption = new("--output-file", "-o")
        {
            Description = "The output file."
        };
        Option<bool> forceOption = new("--force", "-f")
        {
            Description = "Always write the output, even if there is no change."
        };
        Option<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "Write verbose output"
        };

        RootCommand rootCommand = new("Sorts elements in a resx file")
        {
            new Command("no-op", "No operation"),
            inputFileOption,
            outputFileOption,
            forceOption
        };

        rootCommand.SetAction((ParseResult parseResult) =>
        {
            FileInfo inputFile = parseResult.GetValue(inputFileOption)!;
            FileInfo? outputFile = parseResult.GetValue(outputFileOption);
            bool force = parseResult.GetValue(forceOption);
            bool verbose = parseResult.GetValue(verboseOption);

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
        return rootCommand;
    }
}