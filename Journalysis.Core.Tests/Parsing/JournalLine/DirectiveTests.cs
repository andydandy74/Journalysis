using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class DirectiveTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "Directive"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_Directive_ValidatesExpectedResult(
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
            var directive = Assert.IsType<JournalDirectiveLine>(parsingError.JournalLine);

            AssertDirectiveProperties(root, directive);
            AssertJournalMetadata(root, journal);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var directiveLine = Assert.IsType<JournalDirectiveLine>(record);

        AssertDirectiveProperties(root, directiveLine);
        AssertJournalMetadata(root, journal);
    }

    private static void AssertDirectiveProperties(
        JsonElement root,
        JournalDirectiveLine directive)
    {
        if (root.TryGetProperty("Key", out var keyElement))
        {
            if (keyElement.ValueKind == JsonValueKind.Null)
            {
                Assert.Null(directive.Key);
            }
            else
            {
                Assert.Equal(keyElement.GetString(), directive.Key);
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

            Assert.Equal(expectedValues, directive.Values);
        }
    }

    private static void AssertJournalMetadata(
        JsonElement root,
        Journal journal)
    {
        AssertIntProperty(root, "Version", journal.Version);
        AssertStringProperty(root, "User", journal.User);
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
