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

        CliRootCommand rootCommand = new("Sorts elements in a resx file")
        {
            new CliCommand("no-op"),
            inputFileOption,
            outputFileOption
        };

        rootCommand.SetAction((ParseResult parseResult) =>
        {
            FileInfo inputFile = parseResult.CommandResult.GetValue(inputFileOption)!;
            FileInfo? outputFile = parseResult.CommandResult.GetValue(outputFileOption);

            bool needsSort = false;
            using (Stream inputStream = inputFile.OpenRead())
            {
                XDocument document = XDocument.Load(inputStream);
                //Check if the elements are ordered
                string lastName = "";
                foreach (var element in document.Root!.Elements(XName.Get("data")))
                {
                    string resourceName = element.Attributes().Single(x => x.Name.LocalName == "name").Value;
                    if (string.Compare(lastName, resourceName) > 0)
                    {
                        needsSort = true;
                        break;
                    }
                    lastName = resourceName;
                }
            }
            if (needsSort)
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
                    using Stream outputStream = outputFile.OpenWrite();
                    ms.CopyTo(outputStream);
                }
                else
                {
                    using Stream outputStream = inputFile.OpenWrite();
                    ms.CopyTo(outputStream);
                }
            }

        });
        return new CliConfiguration(rootCommand);
    }
}