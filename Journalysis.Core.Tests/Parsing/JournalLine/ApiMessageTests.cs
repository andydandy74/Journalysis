using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class ApiMessageTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "ApiMessage"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_ApiMessage_ValidatesExpectedResult(
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
            var apiMessage = Assert.IsType<JournalApiMessageLine>(
                parsingError.JournalLine);

            if (expected.RootElement.TryGetProperty("IsError", out var isErrorElement))
            {
                Assert.Equal(isErrorElement.GetBoolean(), apiMessage.IsError);
            }

            if (expected.RootElement.TryGetProperty("MessageText", out var messageTextElement))
            {
                var expectedMsg = messageTextElement.GetString();
                if (expectedMsg is null)
                {
                    Assert.Null(apiMessage.MessageText);
                }
                else
                {
                    Assert.Equal(expectedMsg, apiMessage.MessageText);
                }
            }

            if (expected.RootElement.TryGetProperty("MessageType", out var messageTypeElement))
            {
                var expectedTypeString = messageTypeElement.GetString();
                if (Enum.TryParse<JournalApiMessageType>(expectedTypeString, out var expectedType))
                {
                    Assert.Equal(expectedType, apiMessage.MessageType);
                }
            }

            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var apiMessageLine = Assert.IsType<JournalApiMessageLine>(record);

        if (expected.RootElement.TryGetProperty("IsError", out var isErrorNormalElement))
        {
            Assert.Equal(isErrorNormalElement.GetBoolean(), apiMessageLine.IsError);
        }

        if (expected.RootElement.TryGetProperty("MessageText", out var messageTextNormalElement))
        {
            var expectedMsgNormal = messageTextNormalElement.GetString();
            if (expectedMsgNormal is null)
            {
                Assert.Null(apiMessageLine.MessageText);
            }
            else
            {
                Assert.Equal(expectedMsgNormal, apiMessageLine.MessageText);
            }
        }

        if (expected.RootElement.TryGetProperty("MessageType", out var messageTypeNormalElement))
        {
            var expectedTypeString = messageTypeNormalElement.GetString();
            if (Enum.TryParse<JournalApiMessageType>(expectedTypeString, out var expectedType))
            {
                Assert.Equal(expectedType, apiMessageLine.MessageType);
            }
        }
    }
}
