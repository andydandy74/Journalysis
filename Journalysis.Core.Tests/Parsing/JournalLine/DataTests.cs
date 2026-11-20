using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class DataTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "Data"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_Data_ValidatesExpectedResult(
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
            var data = Assert.IsType<JournalDataLine>(parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, data);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var dataLine = Assert.IsType<JournalDataLine>(record);

        AssertExpectedProperties(expected.RootElement, dataLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalDataLine data)
    {
        if (root.TryGetProperty("Key", out var keyElement))
        {
            if (keyElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(data.Key);
            }
            else
            {
                Assert.Equal(keyElement.GetString(), data.Key);
            }
        }

        if (root.TryGetProperty("Values", out var valuesElement))
        {
            var expectedValues = new List<string?>();
            foreach (var value in valuesElement.EnumerateArray())
            {
                expectedValues.Add(value.ValueKind == JsonValueKind.Null
                    ? null
                    : value.GetString());
            }

            Assert.Equal(expectedValues, data.Values);
        }
    }
}
