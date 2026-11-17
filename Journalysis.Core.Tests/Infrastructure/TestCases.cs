namespace Journalysis.Core.Tests.Infrastructure;

/// <summary>
/// Discovers file-based test cases copied into the test output directory.
/// </summary>
internal static class TestCases
{
    private const string InputSuffix = ".input.txt";

    public static IEnumerable<object[]> Discover(string category)
    {
        // Do not rely on the working directory: IDEs and CI runners may use
        // different locations when launching tests.
        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            category);

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                $"Test data directory was not found: {directory}");
        }

        var inputs = Directory
            .EnumerateFiles(directory, $"*{InputSuffix}", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (inputs.Length == 0)
        {
            throw new InvalidOperationException(
                $"No input files were found in: {directory}");
        }

        foreach (var inputPath in inputs)
        {
            var stem = inputPath[..^InputSuffix.Length];
            var expectedPath = stem + ".expected.json";

            if (!File.Exists(expectedPath))
            {
                throw new FileNotFoundException(
                    $"Expected results are missing for '{inputPath}'.",
                    expectedPath);
            }

            // The relative name helps identify a failing case in test results.
            var caseName = Path.GetRelativePath(directory, stem);

            yield return new object[] { caseName, inputPath, expectedPath };
        }
    }
}