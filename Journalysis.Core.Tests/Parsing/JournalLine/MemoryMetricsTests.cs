using System.Text.Json;
using Journalysis.Core.Models;
using Journalysis.Core.Parsing;
using Journalysis.Core.Tests.Infrastructure;

namespace Journalysis.Core.Tests.Parsing.JournalLine;

public sealed class MemoryMetricsTests
{
    public static IEnumerable<object[]> Cases =>
        TestCases.Discover(Path.Combine("JournalLine", "MemoryMetrics"));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Parse_MemoryMetrics_ValidatesExpectedResult(
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
            var memoryMetrics = Assert.IsType<JournalMemoryMetricsLine>(
                parsingError.JournalLine);

            AssertExpectedProperties(root, memoryMetrics);
            return;
        }

        Assert.False(journal.HasParsingErrors);

        var record = Assert.Single(journal.Lines);
        var memoryMetricsLine = Assert.IsType<JournalMemoryMetricsLine>(record);

        AssertExpectedProperties(root, memoryMetricsLine);
    }

    private static void AssertExpectedProperties(
        JsonElement root,
        JournalMemoryMetricsLine line)
    {
        AssertNullableIntProperty(root, "VmAvailable", line.VmAvailable);
        AssertNullableIntProperty(root, "VmUsed", line.VmUsed);
        AssertNullableIntProperty(root, "VmPeak", line.VmPeak);
        AssertNullableIntProperty(root, "RamAvailable", line.RamAvailable);
        AssertNullableIntProperty(root, "RamUsed", line.RamUsed);
        AssertNullableIntProperty(root, "RamPeak", line.RamPeak);
    }

    private static void AssertNullableIntProperty(
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
