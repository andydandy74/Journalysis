using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class MouseEventTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "MouseEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_MouseEvent_ValidatesExpectedResult(
        string caseName,
        string inputPath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        using var expected = JsonDocument.Parse(
            File.ReadAllText(expectedPath));

        var exceptionName = expected.RootElement
            .TryGetProperty("Exception", out var exceptionElement)
                ? exceptionElement.GetString()
                : null;

        var journal = new JournalParser().Parse(inputPath);

        if (string.Equals(exceptionName, nameof(FormatException),
                          StringComparison.Ordinal))
        {
            var parsingError = Assert.Single(journal.ParsingErrors);
            var mouseEvent = Assert.IsType<JournalMouseEventLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, mouseEvent);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var mouseEventLine = Assert.IsType<JournalMouseEventLine>(record);

        AssertExpectedProperties(expected.RootElement, mouseEventLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalMouseEventLine mouseEvent)
    {
        if (root.TryGetProperty("MouseEventType", out var typeElement))
        {
            if (typeElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(mouseEvent.MouseEventType);
            }
            else if (Enum.TryParse<JournalMouseEventType>(
                         typeElement.GetString(), out var expectedType))
            {
                Assert.Equal((JournalMouseEventType?)expectedType,
                             mouseEvent.MouseEventType);
            }
        }

        if (root.TryGetProperty("Data", out var dataElement))
        {
            var expectedData = new List<int>();
            foreach (var value in dataElement.EnumerateArray())
            {
                expectedData.Add(value.GetInt32());
            }

            Assert.Equal(expectedData, mouseEvent.Data);
        }
    }
}
