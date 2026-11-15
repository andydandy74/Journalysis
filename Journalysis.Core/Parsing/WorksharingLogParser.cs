using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Journalysis.Core.Models;

namespace Journalysis.Core.Parsing;

/// <summary>
/// Parses Revit worksharing log (<c>.slog</c>) text into a <see cref="WorksharingLog"/>.
/// </summary>
/// <remarks>
/// A worksharing log consists of:
/// <list type="bullet">
/// <item>a format header, e.g. <c>Worksharing Log Version 2.1, Encoding UTF-16</c>,</item>
/// <item>event lines, e.g. <c>$1a2b3c4d 2024-01-31 09:15:42.123 &gt;Open</c>,</item>
/// <item>indented session metadata lines (<c>user=</c>, <c>build=</c>, <c>journal=</c>,
/// <c>host=</c>, <c>server=</c>, <c>central=</c>) that follow a <c>&gt;Session</c> event.</item>
/// </list>
/// Lines that cannot be parsed are skipped and reported in
/// <see cref="WorksharingLog.ParsingErrors"/>.
/// </remarks>
public sealed class WorksharingLogParser
{
    private const string ReconnectInMiddleMarker = ".ReconnectInMiddle";
    private const string SessionMarker = ">Session";
    private const string VersionHeaderMarker = "Worksharing";
    private const string VersionHeaderPrefix = "Version ";

    /// <summary>
    /// Parses a worksharing log from a file path.
    /// </summary>
    /// <param name="path">Path to a worksharing log file.</param>
    /// <returns>A parsed <see cref="WorksharingLog"/> instance.</returns>
    /// <remarks>
    /// Revit writes worksharing logs as UTF-16 LE. A byte order mark, when present,
    /// takes precedence. This overload owns and disposes the created file reader.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    public WorksharingLog Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var reader = new StreamReader(path, Encoding.Unicode, detectEncodingFromByteOrderMarks: true);
        return Parse(reader);
    }

    /// <summary>
    /// Parses worksharing log text from a <see cref="TextReader"/> starting at its current position.
    /// </summary>
    /// <param name="reader">Source reader positioned at the first line to parse.</param>
    /// <returns>A parsed <see cref="WorksharingLog"/> instance.</returns>
    /// <remarks>
    /// The caller owns the reader. This method does not dispose or rewind it.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <see langword="null"/>.</exception>
    public WorksharingLog Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var processingStarted = Stopwatch.GetTimestamp();

        var sessions = new List<WorksharingSession>();
        var sessionsById = new Dictionary<string, WorksharingSession>(StringComparer.Ordinal);
        var parsingErrors = new List<WorksharingLogParsingError>();
        WorksharingSession? currentSession = null;
        string? version = null;
        var lineNumber = 0;

        while (reader.ReadLine() is { } rawLine)
        {
            lineNumber++;

            // Metadata lines are indented; a BOM may remain if the reader did not consume it.
            var line = rawLine.TrimStart(' ', '\uFEFF');

            try
            {
                if (line.StartsWith('$'))
                {
                    currentSession = ParseEventLine(line, sessions, sessionsById);
                }
                else if (line.StartsWith(VersionHeaderMarker, StringComparison.Ordinal))
                {
                    version = ParseVersionHeader(line);
                }
                else if (TryGetMetadataKey(line) is { } key)
                {
                    // Metadata belongs to the session whose >Session event preceded it.
                    if (currentSession is null)
                    {
                        throw new FormatException("Session metadata found before any session event.");
                    }

                    ParseMetadataLine(key, line, currentSession);
                }
            }
            catch (FormatException exception)
            {
                // Skip the offending line and continue with later lines.
                parsingErrors.Add(new WorksharingLogParsingError(lineNumber, rawLine, exception.Message));
            }
        }

        var log = new WorksharingLog(version, sessions, parsingErrors);
        log.ProcessingTime = Stopwatch.GetElapsedTime(processingStarted);

        foreach (var session in sessions)
        {
            session.FinalizeParsing();
        }

        return log;
    }

    /// <summary>
    /// Parses an event line and appends the event to its (possibly new) session.
    /// </summary>
    /// <returns>The session the event belongs to.</returns>
    /// <exception cref="FormatException">The line does not contain a valid ID and timestamp.</exception>
    private static WorksharingSession ParseEventLine(
        string line,
        List<WorksharingSession> sessions,
        Dictionary<string, WorksharingSession> sessionsById)
    {
        // Format: <sessionId> <date> <time> <event text...>
        var tokens = line.Split((char[]?)null, 4, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3)
        {
            throw new FormatException("Event line must contain a session ID, date, and time.");
        }

        if (!DateTime.TryParse(
                tokens[1] + " " + tokens[2],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var timestamp))
        {
            throw new FormatException($"Invalid event timestamp '{tokens[1]} {tokens[2]}'.");
        }

        var sessionId = tokens[0];
        if (!sessionsById.TryGetValue(sessionId, out var session))
        {
            session = new WorksharingSession(sessionId);
            sessions.Add(session);
            sessionsById.Add(sessionId, session);
        }

        // Collapse internal whitespace runs to single spaces, as the Python implementation did.
        var text = tokens.Length > 3
            ? string.Join(' ', tokens[3].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            : string.Empty;

        session.AddEvent(new WorksharingEvent(timestamp, text));

        if (text.StartsWith(SessionMarker, StringComparison.Ordinal))
        {
            session.Start = timestamp;
        }
        else if (text.StartsWith(ReconnectInMiddleMarker, StringComparison.Ordinal))
        {
            // A reconnected session has no >Session marker in this log, so use its first event.
            session.Start = session.Events[0].DateTime;
        }
        else
        {
            session.End = timestamp;
        }

        return session;
    }

    /// <summary>
    /// Populates session metadata from a <c>key=value</c> line.
    /// </summary>
    /// <exception cref="FormatException">The value is missing or malformed.</exception>
    private static void ParseMetadataLine(string key, string line, WorksharingSession session)
    {
        var value = line[(key.Length + 1)..];

        switch (key)
        {
            case "user":
                session.User = Unquote(value);
                break;

            case "journal":
                session.Journal = Unquote(value);
                break;

            case "central":
                session.Central = Unquote(value);
                break;

            case "build":
                {
                    // Format: build="<version> <build>"; the first token may carry a minor suffix (e.g. 2024.2).
                    var parts = SplitWhitespace(value.Replace("\"", string.Empty, StringComparison.Ordinal));
                    session.RevitBuild = parts[^1];

                    var majorVersion = parts[0].Split('.')[0];
                    if (!int.TryParse(majorVersion, NumberStyles.None, CultureInfo.InvariantCulture, out var revitVersion))
                    {
                        throw new FormatException($"Invalid Revit version '{parts[0]}'.");
                    }

                    session.RevitVersion = revitVersion;
                    break;
                }

            case "host":
                {
                    // Format: host=<address> "<name>"
                    var parts = SplitWhitespace(value);
                    session.HostAddress = parts[0];
                    session.HostName = Unquote(parts[^1]);
                    break;
                }

            case "server":
                {
                    // Format: server=<address> "<name>"
                    var parts = SplitWhitespace(value);
                    session.ServerAddress = parts[0];
                    session.ServerName = Unquote(parts[^1]);
                    break;
                }
        }
    }

    /// <summary>
    /// Extracts the format version from the log header line.
    /// </summary>
    private static string? ParseVersionHeader(string line)
    {
        var index = line.LastIndexOf(VersionHeaderPrefix, StringComparison.Ordinal);
        if (index < 0)
        {
            return null;
        }

        return line[(index + VersionHeaderPrefix.Length)..].Split(',')[0];
    }

    /// <exception cref="FormatException">The value contains no tokens.</exception>
    private static string[] SplitWhitespace(string value)
    {
        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            throw new FormatException("Metadata value is empty.");
        }

        return parts;
    }

    /// <summary>
    /// Returns the metadata key when the line has the form <c>key=...</c> for a known key.
    /// </summary>
    private static string? TryGetMetadataKey(string line)
    {
        foreach (var key in new[] { "user", "build", "journal", "host", "server", "central" })
        {
            if (line.Length > key.Length
                && line.StartsWith(key, StringComparison.Ordinal)
                && line[key.Length] == '=')
            {
                return key;
            }
        }

        return null;
    }

    private static string Unquote(string value) => value.Replace("\"", string.Empty, StringComparison.Ordinal);
}
