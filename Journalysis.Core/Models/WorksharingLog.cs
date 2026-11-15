using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents a parsed Revit worksharing log (<c>.slog</c> file).
/// </summary>
/// <remarks>
/// The constructor snapshots the provided session collection. Session order is preserved as parsed.
/// </remarks>
public sealed class WorksharingLog
{
    /// <summary>
    /// Initializes a parsed worksharing log.
    /// </summary>
    /// <param name="version">Worksharing log format version, when available.</param>
    /// <param name="sessions">Parsed sessions in encounter order.</param>
    /// <param name="parsingErrors">
    /// Recoverable parsing errors, or <see langword="null"/> when no errors were reported.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="sessions"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sessions"/> or <paramref name="parsingErrors"/> contains null entries.
    /// </exception>
    public WorksharingLog(
        string? version,
        IReadOnlyList<WorksharingSession> sessions,
        IReadOnlyList<WorksharingLogParsingError>? parsingErrors = null)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        // Take a snapshot so later external collection changes do not affect this log.
        var snapshot = sessions.ToArray();

        if (snapshot.Any(session => session is null))
        {
            throw new ArgumentException("Sessions cannot contain null entries.", nameof(sessions));
        }

        var parsingErrorSnapshot = parsingErrors?.ToArray() ?? Array.Empty<WorksharingLogParsingError>();

        if (parsingErrorSnapshot.Any(error => error is null))
        {
            throw new ArgumentException("Parsing errors cannot contain null entries.", nameof(parsingErrors));
        }

        Version = version;
        Sessions = Array.AsReadOnly(snapshot);
        ParsingErrors = Array.AsReadOnly(parsingErrorSnapshot);
    }

    private bool? _allSessionsUseSameBuild;
    private bool? _hasParsingErrors;

    /// <summary>
    /// Gets whether all sessions were recorded with the same Revit build.
    /// The result is computed on first access and then cached.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> when the log contains no sessions.
    /// </remarks>
    public bool AllSessionsUseSameBuild =>
        _allSessionsUseSameBuild ??= Sessions
            .Select(x => x.RevitBuild)
            .Distinct(StringComparer.Ordinal)
            .Count() == 1;

    /// <summary>
    /// Gets the first session with the specified identifier.
    /// </summary>
    /// <param name="id">Session identifier, for example <c>$1a2b3c4d</c>.</param>
    /// <returns>The matching session, or <see langword="null"/> when none is found.</returns>
    public WorksharingSession? GetSessionById(string id) =>
        Sessions.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    /// <summary>
    /// Gets whether any recoverable parsing errors were reported.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool HasParsingErrors =>
        _hasParsingErrors ??= ParsingErrors.Count > 0;

    /// <summary>
    /// Gets recoverable parsing errors in encounter order.
    /// </summary>
    public IReadOnlyList<WorksharingLogParsingError> ParsingErrors { get; }

    /// <summary>
    /// Gets the time spent parsing the log file, when measured.
    /// </summary>
    public TimeSpan? ProcessingTime { get; internal set; }

    /// <summary>
    /// Gets the number of sessions in this log.
    /// </summary>
    public int SessionCount => Sessions.Count;

    /// <summary>
    /// Gets the parsed sessions in encounter order.
    /// </summary>
    public IReadOnlyList<WorksharingSession> Sessions { get; }

    public override string ToString() => nameof(WorksharingLog);

    /// <summary>
    /// Gets the worksharing log format version, when available.
    /// </summary>
    public string? Version { get; }
}
