using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class GuiResourceUsageTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "GuiResourceUsage"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_GuiResourceUsage_ValidatesExpectedResult(
        string caseName,
        string inputPath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        using var expected = JsonDocument.Parse(
            File.ReadAllText(expectedPath));

        var root = expected.RootElement;

        var exceptionName = root.TryGetProperty("Exception", out var exceptionElement)
            ? exceptionElement.GetString()
            : null;

        var journal = new JournalParser().Parse(inputPath);

        if (string.Equals(exceptionName, nameof(FormatException),
                           StringComparison.Ordinal))
        {
            var parsingError = Assert.Single(journal.ParsingErrors);
            var guiResourceUsage = Assert.IsType<JournalGuiResourceUsageLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(root, guiResourceUsage);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var guiResourceUsageLine = Assert.IsType<JournalGuiResourceUsageLine>(record);

        AssertExpectedProperties(root, guiResourceUsageLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalGuiResourceUsageLine line)
    {
        AssertNullableIntProperty(root, "Available", line.Available);
        AssertNullableIntProperty(root, "Used", line.Used);
        AssertNullableIntProperty(root, "User", line.User);
    }

    private static void AssertNullableIntProperty(
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
