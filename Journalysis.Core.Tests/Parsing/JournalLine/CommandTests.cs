using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class CommandTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "Command"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_Command_ValidatesExpectedResult(
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
            var command = Assert.IsType<JournalCommandLine>(parsingError.JournalLine);

            AssertExpectedProperties(expected.RootElement, command);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var commandLine = Assert.IsType<JournalCommandLine>(record);

        AssertExpectedProperties(expected.RootElement, commandLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalCommandLine command)
    {
        if (root.TryGetProperty("CommandType", out var commandTypeElement))
        {
            var typeString = commandTypeElement.GetString();
            if (Enum.TryParse<JournalCommandType>(typeString, out var expectedType))
            {
                Assert.Equal(expectedType, command.CommandType);
            }
        }

        AssertStringProperty(root, "CommandDescription", command.CommandDescription);
        AssertStringProperty(root, "CommandId", command.CommandId);
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
