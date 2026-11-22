using System.Globalization;
using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class TimeStampTests
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "TimeStamp"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_TimeStamp_ValidatesExpectedResult(
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
            var timeStamp = Assert.IsType<JournalTimeStampLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, timeStamp);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var timeStampLine = Assert.IsType<JournalTimeStampLine>(record);

        AssertExpectedProperties(expected.RootElement, timeStampLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalTimeStampLine timeStamp)
    {
        if (root.TryGetProperty("TimeStampType", out var timeStampTypeElement))
        {
            if (timeStampTypeElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(timeStamp.TimeStampType);
            }
            else if (Enum.TryParse<JournalTimeStampType>(
                         timeStampTypeElement.GetString(), out var expectedType))
            {
                Assert.Equal((JournalTimeStampType?)expectedType,
                             timeStamp.TimeStampType);
            }
        }

        if (root.TryGetProperty("DebugInfoType", out var debugInfoTypeElement))
        {
            if (debugInfoTypeElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(timeStamp.DebugInfoType);
            }
            else if (Enum.TryParse<JournalDebugInfoType>(
                         debugInfoTypeElement.GetString(), out var expectedDebugInfoType))
            {
                Assert.Equal((JournalDebugInfoType?)expectedDebugInfoType,
                             timeStamp.DebugInfoType);
            }
        }

        AssertStringProperty(root, "Description", timeStamp.Description);

        if (!root.TryGetProperty("DateTime", out var dateTimeElement))
        {
            return;
        }

        if (dateTimeElement.ValueKind == JsonValueKind.Null)
        {
            Assert.Null(timeStamp.DateTime);
            return;
        }

        var expectedDateTime = DateTime.ParseExact(
            dateTimeElement.GetString()!,
            DateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);

        Assert.Equal<DateTime?>(expectedDateTime, timeStamp.DateTime);
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
