using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class BasicFileInfoTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "BasicFileInfo"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_BasicFileInfo_ValidatesExpectedResult(
        string caseName,
        string inputPath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        using var expected = JsonDocument.Parse(
            File.ReadAllText(expectedPath));

        var journal = new JournalParser().Parse(inputPath);

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var basicFileInfo = Assert.IsType<JournalBasicFileInfoLine>(record);

        var root = expected.RootElement;

        AssertStringProperty(root, "Worksharing", basicFileInfo.Worksharing);
        AssertStringProperty(root, "CentralModelPath", basicFileInfo.CentralModelPath);
        AssertStringProperty(root, "LastSavePath", basicFileInfo.LastSavePath);
        AssertStringProperty(root, "FileName", basicFileInfo.FileName);
        AssertStringProperty(root, "Locale", basicFileInfo.Locale);
        AssertStringProperty(root, "FileExtension", basicFileInfo.FileExtension);
        AssertStringProperty(root, "Build", basicFileInfo.Build);
        AssertIntProperty(root, "Version", basicFileInfo.Version);
    }

    private static void AssertStringProperty(
        JsonElement root,
        string name,
        string? actual)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return;
        }

        var expectedValue = element.GetString();
        if (expectedValue is null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.Equal(expectedValue, actual);
        }
    }

    private static void AssertIntProperty(
        JsonElement root,
        string name,
        int? actual)
    {
        if (!root.TryGetProperty(name, out var element))
        {
            return;
        }

        if (element.ValueKind == JsonValueKind.Null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.Equal(element.GetInt32(), actual);
        }
    }
}
