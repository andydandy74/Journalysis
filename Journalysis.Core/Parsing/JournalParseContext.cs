namespace Journalysis.Core.Parsing;

/// <summary>
/// Holds journal metadata and shared state collected during the second pass.
/// A new instance is created for every parse operation.
/// </summary>
internal sealed class JournalParseContext
{
    public int? Version { get; set; }
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the journal path recorded inside the journal text.
    /// This may differ from the location of the file being parsed.
    /// </summary>
    public string? RecordedPath { get; set; }

    public string? Build { get; set; }
    public string? Branch { get; set; }
    public string? Release { get; set; }
    public string? MachineName { get; set; }
    public string? OsVersion { get; set; }

    // Preserve the original parser's first-match behavior for these fields.
    public bool MachineNameFound { get; set; }
    public bool OsVersionFound { get; set; }

    // The original parser increments this across system-information records.
    public int SystemInformationItemNumber { get; set; }
}