using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class KeyboardEventTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "KeyboardEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_KeyboardEvent_ValidatesExpectedResult(
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
            var keyboardEvent = Assert.IsType<JournalKeyboardEventLine>(
                parsingError.JournalLine);

            AssertExpectedKey(expected.RootElement, keyboardEvent);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var keyboardEventLine = Assert.IsType<JournalKeyboardEventLine>(record);

        AssertExpectedKey(expected.RootElement, keyboardEventLine);
    }

    private static void AssertExpectedKey(
        JsonElement root,
        JournalKeyboardEventLine keyboardEvent)
    {
        if (root.TryGetProperty("Key", out var keyElement))
        {
            if (keyElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(keyboardEvent.Key);
            }
            else
            {
                Assert.Equal(keyElement.GetString(), keyboardEvent.Key);
            }
        }
    }
}
