using System.CommandLine;
using System.Data;
using System.Xml.Linq;

namespace ResxSorter.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Invoke_WithHelpOption_DisplaysHelp()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("--help", stdOut);
        
        Assert.Equal(0, exitCode);
        Assert.Contains("--help", stdOut.ToString());
    }

    [Fact]
    public async Task Invoke_WithOutputFile_SortsElementsIntoNewFile()
    {
        using StringWriter stdOut = new();
        int exitCode = await Invoke("-i Resources.resx -o TestOutput.resx", stdOut);

        Assert.Equal(0, exitCode);
        AssertIsSorted("TestOutput.resx");
    }

    [Fact]
    public async Task Invoke_WithoutOutputFile_SortsElementsIntoSourceFile()
    {
        File.Copy("Resources.resx", "Resources.Dup.resx", true);
        using StringWriter stdOut = new();
        int exitCode = await Invoke("-i Resources.Dup.resx", stdOut);

        Assert.Equal(0, exitCode);
        AssertIsSorted("Resources.Dup.resx");
    }

    [Fact]
    public async Task Invoke_WithSortedFile_DoesNothing()
    {
        using StringWriter stdOut = new();

        DateTime lastWriteTime = File.GetLastWriteTime("Resources.Sorted.resx");

        int exitCode = await Invoke("-i Resources.Sorted.resx", stdOut);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists("Resources.Sorted.resx"));
        
        DateTime afterLastWriteTime = File.GetLastWriteTime("Resources.Sorted.resx");
        Assert.Equal(lastWriteTime, afterLastWriteTime);
    }

    [Fact]
    public async Task Invoke_WithSortedFileAndOutputFile_OutputsToNewFile()
    {
        using StringWriter stdOut = new();

        DateTime lastWriteTime = File.GetLastWriteTime("Resources.Sorted.resx");

        int exitCode = await Invoke("-i Resources.Sorted.resx -o MyResources.Sorted.resx", stdOut);

        Assert.Equal(0, exitCode);
        AssertIsSorted("MyResources.Sorted.resx");
        Assert.Equal(lastWriteTime, File.GetLastWriteTime("Resources.Sorted.resx"));
    }

    private static Task<int> Invoke(string commandLine, StringWriter console)
    {
        CliConfiguration configuration = Program.GetConfiguration();
        configuration.Output = console;
        return configuration.InvokeAsync(commandLine);
    }

    private static void AssertIsSorted(string filePath)
    {
        Assert.True(File.Exists(filePath));
        using Stream outputStream = File.OpenRead(filePath);
        XDocument doc = XDocument.Load(outputStream);
        var names = doc.Descendants().Where(x => x.Name.LocalName == "data")
            .Select(x => x.Attributes().Single(x => x.Name.LocalName == "name").Value)
            .ToList();

        var sorted = names.OrderBy(x => x).ToList();
        Assert.Equal(sorted, names);
    }
}