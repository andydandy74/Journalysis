using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class AddinEventTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "AddinEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_AddinEvent_ValidatesExpectedResult(
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
            var addinEvent = Assert.IsType<JournalAddinEventLine>(
                parsingError.JournalLine);

            var expectedMessage = expected.RootElement
                .GetProperty("MessageText")
                .GetString();

            Assert.Equal(expectedMessage, addinEvent.MessageText);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var addinEventLine = Assert.IsType<JournalAddinEventLine>(record);

        var expectedMessageNormal = expected.RootElement
            .GetProperty("MessageText")
            .GetString();

        Assert.Equal(expectedMessageNormal, addinEventLine.MessageText);
    }
}