using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class ExternalEventTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "ExternalEvent"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_ExternalEvent_ValidatesExpectedResult(
        string caseName,
        string inputPath,
        string expectedPath)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        using var expected = JsonDocument.Parse(
            File.ReadAllText(expectedPath));

        var journal = new JournalParser().Parse(inputPath);

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var externalEvent = Assert.IsType<JournalExternalEventLine>(record);

        var root = expected.RootElement;

        AssertStringProperty(root, "AddInId", externalEvent.AddInId);
        AssertStringProperty(root, "AddInName", externalEvent.AddInName);
        AssertStringProperty(root, "AddInVersion", externalEvent.AddInVersion);
        AssertStringProperty(root, "ExternalEventName", externalEvent.ExternalEventName);
        AssertStringProperty(root, "VendorDescription", externalEvent.VendorDescription);
        AssertStringProperty(root, "VendorId", externalEvent.VendorId);
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
