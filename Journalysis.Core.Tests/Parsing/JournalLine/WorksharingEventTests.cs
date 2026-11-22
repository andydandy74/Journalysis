using System.Globalization;
using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class WorksharingEventTests
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "WorksharingEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_WorksharingEvent_ValidatesExpectedResult(
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
            var worksharingEvent = Assert.IsType<JournalWorksharingEventLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, worksharingEvent);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var worksharingEventLine = Assert.IsType<JournalWorksharingEventLine>(record);

        AssertExpectedProperties(expected.RootElement, worksharingEventLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalWorksharingEventLine worksharingEvent)
    {
        AssertStringProperty(root, "SessionId", worksharingEvent.SessionId);
        AssertStringProperty(root, "Text", worksharingEvent.Text);

        if (!root.TryGetProperty("DateTime", out var dateTimeElement))
        {
            return;
        }

        if (dateTimeElement.ValueKind == JsonValueKind.Null)
        {
            Assert.Null(worksharingEvent.DateTime);
            return;
        }

        var expectedDateTime = DateTime.ParseExact(
            dateTimeElement.GetString()!,
            DateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        Assert.Equal<DateTime?>(expectedDateTime, worksharingEvent.DateTime);
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
}
