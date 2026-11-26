using Journalysis.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents one session recorded in a worksharing log.
/// </summary>
/// <remarks>
/// Session metadata (user, build, host, server, and so on) is populated by the parser
/// from header lines that follow the session's first event.
/// </remarks>
public sealed class WorksharingSession
{
    // Event markers used by the analysis helpers below.
    private const string OpenStart = ">Open";
    private const string OpenEnd = "<Open";
    private const string OpenCentral = ">Open:Central";
    private const string OpenLinkStart = ">OpenLink";
    private const string OpenLinkEnd = "<OpenLink";
    private const string ReconnectInMiddle = ".ReconnectInMiddle";
    private const string SyncStart = ">STC";
    private const string SyncEnd = "<STC";
    private const string SyncReloadLatest = ">STC:RL:Read";
    private const string SyncAborted = ".STC:RL:LockRoot RW gaveUp";
    private const string WorksetConfigStart = ">WSConfig";
    private const string WorksetConfigEnd = "<WSConfig";
    private const string WorksetDialog = ">WSD";

    /// <summary>
    /// Initializes a worksharing session.
    /// </summary>
    /// <param name="id">Session identifier, for example <c>$1a2b3c4d</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> is <see langword="null"/>.</exception>
    public WorksharingSession(string id)
    {
        ArgumentNullException.ThrowIfNull(id);

        Id = id;
    }

    private readonly List<WorksharingEvent> _events = new();
    private IReadOnlyList<LoadedLink>? _loadedLinks;
    private IReadOnlyList<SyncEvent>? _syncEvents;

    /// <summary>
    /// Gets the central model path, when available.
    /// </summary>
    public string? Central { get; internal set; }

    /// <summary>
    /// Gets the calendar date on which the session started, when available.
    /// </summary>
    public DateTime? Date => Start?.Date;

    /// <summary>
    /// Gets the session duration, when both start and end are known.
    /// </summary>
    public TimeSpan? Duration => Start is { } start && End is { } end ? end - start : null;

    /// <summary>
    /// Gets the timestamp of the last non-start event in this session, when available.
    /// </summary>
    public DateTime? End { get; internal set; }

    /// <summary>
    /// Gets the number of events recorded for this session.
    /// </summary>
    public int EventCount => Events.Count;

    /// <summary>
    /// Gets the events recorded for this session in encounter order.
    /// </summary>
    public IReadOnlyList<WorksharingEvent> Events => _events;

    /// <summary>
    /// Gets whether at least one link finished loading in this session.
    /// </summary>
    public bool HasLoadedLinks => ContainsEvent(OpenLinkEnd);

    /// <summary>
    /// Gets the host IP address, when available.
    /// </summary>
    public string? HostAddress { get; internal set; }

    /// <summary>
    /// Gets the host machine name, when available.
    /// </summary>
    public string? HostName { get; internal set; }

    /// <summary>
    /// Gets the session identifier, for example <c>$1a2b3c4d</c>.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the path of the journal file associated with this session, when available.
    /// </summary>
    public string? Journal { get; internal set; }

    /// <summary>
    /// Gets the number of loaded links derived from this session's events.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public int LoadedLinkCount => LoadedLinks.Count;

    /// <summary>
    /// Gets loaded-link records derived from <c>&gt;OpenLink</c> / <c>&lt;OpenLink</c> event pairs.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public IReadOnlyList<LoadedLink> LoadedLinks =>
        _loadedLinks ??= (IReadOnlyList<LoadedLink>)LoadedLinkAnalyzer.GetLoadedLinks(this);

    /// <summary>
    /// Gets the Revit build string, when available.
    /// </summary>
    public string? RevitBuild { get; internal set; }

    /// <summary>
    /// Gets the Revit major version (for example 2024), when available.
    /// </summary>
    public int? RevitVersion { get; internal set; }

    /// <summary>
    /// Gets the server IP address, when available.
    /// </summary>
    public string? ServerAddress { get; internal set; }

    /// <summary>
    /// Gets the server name, when available.
    /// </summary>
    public string? ServerName { get; internal set; }

    /// <summary>
    /// Classifies this session based on its recorded events.
    /// </summary>
    public WorksharingSessionType SessionType { get; private set; } =
        WorksharingSessionType.Unknown;

    /// <summary>
    /// Gets the session start timestamp, when available.
    /// </summary>
    public DateTime? Start { get; internal set; }

    /// <summary>
    /// Gets the number of synchronize-with-central operations derived from this session's events.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public int SyncEventCount => SyncEvents.Count;

    /// <summary>
    /// Gets synchronize-with-central records derived from <c>&gt;STC</c> / <c>&lt;STC</c> event pairs.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public IReadOnlyList<SyncEvent> SyncEvents =>
        _syncEvents ??= (IReadOnlyList<SyncEvent>)SyncEventAnalyzer.GetSyncEvents(this);

    public override string ToString() => nameof(WorksharingSession);

    /// <summary>
    /// Gets the username, when available.
    /// </summary>
    public string? User { get; internal set; }

    /// <summary>
    /// Returns whether an end timestamp was recorded for this session.
    /// </summary>
    public bool WasTerminatedProperly => End is not null;

    internal void AddEvent(WorksharingEvent worksharingEvent) =>
        _events.Add(worksharingEvent);

    private bool ContainsEvent(string text) => Events.Any(x => x.Text == text);

    private WorksharingSessionType DetermineSessionType()
    {
        var containsOpen = ContainsEvent(OpenStart);
        var containsOpenCentral = ContainsEvent(OpenCentral);
        var containsSync = ContainsEvent(SyncStart);
        var containsWorksetDialog = ContainsEvent(WorksetDialog);
        var containsReconnect = Events.Any(
            x => x.Text.StartsWith(ReconnectInMiddle, StringComparison.Ordinal));

        if (containsReconnect)
        {
            return WorksharingSessionType.Reconnected;
        }

        if (!containsOpen && containsSync)
        {
            return WorksharingSessionType.CreateNewCentral;
        }

        if (containsOpenCentral)
        {
            return Events.Count > 1 && Events[1].Text == OpenStart
                ? WorksharingSessionType.CreateDetached
                : WorksharingSessionType.WorkInCentral;
        }

        if (!containsOpen && containsWorksetDialog)
        {
            return WorksharingSessionType.ChooseWorksets;
        }

        if (containsOpen)
        {
            return WorksharingSessionType.CreateLocalCopy;
        }

        return WorksharingSessionType.Unknown;
    }

    internal void FinalizeParsing() =>
        SessionType = DetermineSessionType();

    private DateTime? FirstEventTime(string text) =>
        Events.FirstOrDefault(x => x.Text == text)?.DateTime;
}
