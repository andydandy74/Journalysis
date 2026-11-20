using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class CommentTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "Comment"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_Comment_ValidatesExpectedResult(
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
            Assert.IsType<JournalCommentLine>(parsingError.JournalLine);
        }
        else
        {
            Assert.False(journal.HasParsingErrors);
        }

        AssertExpectedCommentLines(root, journal);

        AssertStringProperty(root, "Path", journal.Path);
        AssertStringProperty(root, "Build", journal.Build);
        AssertStringProperty(root, "Branch", journal.Branch);
        AssertStringProperty(root, "Release", journal.Release);
        AssertStringProperty(root, "Machine", journal.Machine);
    }

    private static void AssertExpectedCommentLines(JsonElement root, Journal journal)
    {
        if (!root.TryGetProperty("CommentLineCount", out var countElement))
        {
            return;
        }

        var commentLines = journal.Lines.OfType<JournalCommentLine>().ToArray();

        Assert.Equal(countElement.GetInt32(), commentLines.Length);

        if (root.TryGetProperty("RawTexts", out var rawTextsElement))
        {
            var expectedRawTexts = rawTextsElement.EnumerateArray()
                .Select(element => element.GetString())
                .ToArray();

            Assert.Equal(expectedRawTexts, commentLines.Select(x => x.RawText).ToArray());
        }
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

        if (element.ValueKind == JsonValueKind.Null)
        {
            Assert.Null(actual);
        }
        else
        {
            Assert.Equal(element.GetString(), actual);
        }
    }
}
