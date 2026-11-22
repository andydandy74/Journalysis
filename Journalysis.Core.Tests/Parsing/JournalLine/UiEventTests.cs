using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class UiEventTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "UiEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_UiEvent_ValidatesExpectedResult(
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
            var uiEvent = Assert.IsType<JournalUiEventLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, uiEvent);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var uiEventLine = Assert.IsType<JournalUiEventLine>(record);

        AssertExpectedProperties(expected.RootElement, uiEventLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalUiEventLine uiEvent)
    {
        if (root.TryGetProperty("UiEventType", out var typeElement))
        {
            if (typeElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(uiEvent.UiEventType);
            }
            else if (Enum.TryParse<JournalUiEventType>(
                         typeElement.GetString(), out var expectedType))
            {
                Assert.Equal((JournalUiEventType?)expectedType,
                             uiEvent.UiEventType);
            }
        }

        if (root.TryGetProperty("Data", out var dataElement))
        {
            var expectedData = new List<string>();
            foreach (var value in dataElement.EnumerateArray())
            {
                expectedData.Add(value.GetString()!);
            }

            Assert.Equal(expectedData, uiEvent.Data);
        }
    }
}
