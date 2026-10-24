using System.Collections.Generic;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Holds journal metadata and shared parse state collected during parser pass 2.
/// </summary>
/// <remarks>
/// A new instance is created for each parse operation and is passed to
/// line-specific parsers so they can contribute shared metadata.
/// </remarks>
internal sealed class JournalParseContext
{
    /// <summary>
    /// Gets or sets the parsed branch token.
    /// </summary>
    public string? Branch { get; set; }

    /// <summary>
    /// Gets or sets the parsed build token.
    /// </summary>
    public string? Build { get; set; }

    /// <summary>
    /// Gets or sets the parsed machine name.
    /// </summary>
    public string? MachineName { get; set; }

    /// <summary>
    /// Gets or sets whether machine name has already been captured.
    /// </summary>
    /// <remarks>
    /// Preserves first-match behavior from the original parser.
    /// </remarks>
    public bool MachineNameFound { get; set; }

    /// <summary>
    /// Gets or sets the parsed operating-system version text.
    /// </summary>
    public string? OsVersion { get; set; }

    /// <summary>
    /// Gets or sets whether OS version has already been captured.
    /// </summary>
    /// <remarks>
    /// Preserves first-match behavior from the original parser.
    /// </remarks>
    public bool OsVersionFound { get; set; }

    /// <summary>
    /// Gets or sets the journal path recorded inside journal text.
    /// </summary>
    /// <remarks>
    /// This may differ from the file-system path used as parser input.
    /// </remarks>
    public string? RecordedPath { get; set; }

    /// <summary>
    /// Gets or sets the parsed release token.
    /// </summary>
    public string? Release { get; set; }

    /// <summary>
    /// Gets or sets the running item number for system-information records.
    /// </summary>
    /// <remarks>
    /// This counter is incremented across qualifying system-information entries
    /// and is intentionally shared across section types.
    /// </remarks>
    public int SystemInformationItemNumber { get; set; }

    /// <summary>
    /// Gets or sets the parsed username (typically from directive records).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the parsed Revit version year (typically from directive records).
    /// </summary>
    public int? Version { get; set; }
}