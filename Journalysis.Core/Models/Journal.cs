using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Analysis;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents a parsed Revit journal and exposes analysis-oriented query helpers.
/// </summary>
/// <remarks>
/// The constructor snapshots the provided line collection and assigns each line's
/// <see cref="JournalLine.Journal"/> and <see cref="JournalLine.Index"/> values.
/// Line order is preserved as parsed.
/// </remarks>
public sealed class Journal
{
    /// <summary>
    /// Initializes a parsed journal instance from parser output and extracted metadata.
    /// </summary>
    /// <param name="lines">Parsed logical journal records in encounter order.</param>
    /// <param name="version">Revit version extracted from directives, when available.</param>
    /// <param name="user">Username extracted from directives, when available.</param>
    /// <param name="blockCount">Total number of timestamp-delimited blocks.</param>
    /// <param name="path">
    /// Journal path recorded in journal comments. This may differ from the physical
    /// file path used during parsing.
    /// </param>
    /// <param name="build">Revit build token extracted from build metadata, when available.</param>
    /// <param name="branch">Revit branch token extracted from build metadata, when available.</param>
    /// <param name="release">Revit release token extracted from build metadata, when available.</param>
    /// <param name="machine">Machine name extracted from journal comments, when available.</param>
    /// <param name="osVersion">OS version extracted from system-information sections, when available.</param>
    /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="lines"/> contains null entries, duplicate object instances,
    /// or line instances already assigned to a different journal.
    /// </exception>
    public Journal(
        IReadOnlyList<JournalLine> lines,
        int? version,
        string? user,
        int blockCount,
        string? path,
        string? build,
        string? branch,
        string? release,
        string? machine,
        string? osVersion)
    {
        ArgumentNullException.ThrowIfNull(lines);

        // Take a snapshot so later external collection changes do not affect navigation.
        var snapshot = lines.ToArray();

        if (snapshot.Any(line => line is null))
        {
            throw new ArgumentException("Journal lines cannot contain null entries.", nameof(lines));
        }

        if (snapshot.Distinct(ReferenceEqualityComparer.Instance).Count() != snapshot.Length)
        {
            throw new ArgumentException("The same line instance cannot appear more than once.", nameof(lines));
        }

        if (snapshot.Any(line => line.Journal is not null))
        {
            throw new ArgumentException("A line cannot belong to more than one journal.", nameof(lines));
        }

        Lines = Array.AsReadOnly(snapshot);
        Version = version;
        User = user;
        BlockCount = blockCount;
        Path = path;
        Build = build;
        Branch = branch;
        Release = release;
        Machine = machine;
        OsVersion = osVersion;

        for (var index = 0; index < snapshot.Length; index++)
        {
            snapshot[index].Index = index;
            snapshot[index].Journal = this;
        }
    }

    private bool? _containsApiErrors;
    private DateTime? _date;
    private bool _dateInitialized;
    private bool? _isInPlaybackMode;
    private IReadOnlyList<LoadedAssembly>? _loadedAssemblies;
    private IReadOnlyList<LoadedModel>? _loadedModels;
    private int? _maxRamPeak;
    private bool _maxRamPeakInitialized;
    private int? _maxVmPeak;
    private bool _maxVmPeakInitialized;
    private int? _minRamAvailable;
    private bool _minRamAvailableInitialized;
    private int? _minVmAvailable;
    private bool _minVmAvailableInitialized;
    private IReadOnlyList<RecordedException>? _recordedExceptions;
    private TimeSpan? _sessionTime;
    private bool _sessionTimeInitialized;
    private TimeSpan? _startupTime;
    private bool _startupTimeInitialized;
    private bool? _wasPlaybackInterrupted;
    private bool? _wasSessionTerminatedProperly;

    /// <summary>
    /// Gets all parsed logical records in encounter order.
    /// </summary>
    /// <remarks>
    /// This is a read-only snapshot created at construction time.
    /// </remarks>
    public IReadOnlyList<JournalLine> Lines { get; }

    /// <summary>
    /// Gets the parsed Revit version year, when available.
    /// </summary>
    public int? Version { get; }

    /// <summary>
    /// Gets the parsed username, when available.
    /// </summary>
    public string? User { get; }

    /// <summary>
    /// Gets the total number of timestamp-delimited blocks found during parsing.
    /// </summary>
    public int BlockCount { get; }

    /// <summary>
    /// Gets the journal path recorded inside the journal content, when available.
    /// </summary>
    public string? Path { get; }

    /// <summary>
    /// Gets the parsed build token, when available.
    /// </summary>
    public string? Build { get; }

    /// <summary>
    /// Gets the parsed branch token, when available.
    /// </summary>
    public string? Branch { get; }

    /// <summary>
    /// Gets the parsed release token, when available.
    /// </summary>
    public string? Release { get; }

    /// <summary>
    /// Gets the parsed machine name, when available.
    /// </summary>
    public string? Machine { get; }

    /// <summary>
    /// Gets the parsed operating system version text, when available.
    /// </summary>
    public string? OsVersion { get; }

    /// <summary>
    /// Gets total parse-processing time when assigned by the parser.
    /// </summary>
    public TimeSpan? ProcessingTime { get; internal set; }

    /// <summary>
    /// Gets the number of parsed logical records, not the physical file line count.
    /// </summary>
    public int LineCount => Lines.Count;

    public override string ToString() => nameof(Journal);

    /// <summary>
    /// Gets whether the journal contains API error messages.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool ContainsApiErrors =>
        _containsApiErrors ??= GetLines<JournalApiMessageLine>().Any(x => x.IsError);

    /// <summary>
    /// Gets whether any recorded exceptions were identified in the journal.
    /// </summary>
    /// <remarks>
    /// This property uses <see cref="RecordedExceptions"/> and therefore triggers
    /// lazy exception analysis on first access.
    /// </remarks>
    public bool ContainsExceptions => RecordedExceptions.Count > 0;

    /// <summary>
    /// Gets the journal timestamp from block 1.
    /// The value is computed on first access and then cached,
    /// including when the result is <see langword="null"/>.
    /// </summary>
    public DateTime? Date
    {
        get
        {
            if (!_dateInitialized)
            {
                _date = GetDateTimeByBlock(1);
                _dateInitialized = true;
            }

            return _date;
        }
    }

    /// <summary>
    /// Gets the timestamp associated with a specific block.
    /// </summary>
    /// <param name="block">The block number to query.</param>
    /// <returns>
    /// The block timestamp, or <see langword="null"/> when the block is 0 or no
    /// timestamp line exists for the requested block.
    /// </returns>
    public DateTime? GetDateTimeByBlock(int block)
    {
        if (block == 0)
        {
            return null;
        }

        return GetLines<JournalTimeStampLine>()
            .FirstOrDefault(x => x.Block == block)?
            .DateTime;
    }

    /// <summary>
    /// Gets timestamps for a set of blocks.
    /// </summary>
    /// <param name="blocks">The blocks to query.</param>
    /// <returns>
    /// A timestamp snapshot for matching blocks, or <see langword="null"/> when
    /// the requested set contains block 0.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="blocks"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Only timestamp lines with a non-null <see cref="JournalTimeStampLine.DateTime"/>
    /// are returned.
    /// </remarks>
    public IReadOnlyList<DateTime>? GetDateTimeByBlocks(IEnumerable<int> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var blockSet = blocks.ToHashSet();
        if (blockSet.Contains(0))
        {
            return null;
        }

        return GetLines<JournalTimeStampLine>()
            .Where(x => x.DateTime.HasValue && blockSet.Contains(x.Block))
            .Select(x => x.DateTime!.Value)
            .ToArray();
    }

    /// <summary>
    /// Gets the first <paramref name="number"/> parsed records.
    /// </summary>
    /// <param name="number">The number of records to return.</param>
    /// <returns>A snapshot containing up to <paramref name="number"/> records.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative.</exception>
    public IReadOnlyList<JournalLine> GetFirstLines(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        return Lines.Take(number).ToArray();
    }

    /// <summary>
    /// Gets the last <paramref name="number"/> parsed records.
    /// </summary>
    /// <param name="number">The number of records to return.</param>
    /// <returns>A snapshot containing up to <paramref name="number"/> records.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is negative.</exception>
    public IReadOnlyList<JournalLine> GetLastLines(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        return Lines.Skip(Math.Max(0, Lines.Count - number)).ToArray();
    }

    /// <summary>
    /// Gets all records of a specific journal-line subtype.
    /// </summary>
    /// <typeparam name="TLine">The requested journal-line subtype.</typeparam>
    /// <returns>A snapshot of matching records in encounter order.</returns>
    public IReadOnlyList<TLine> GetLines<TLine>() where TLine : JournalLine =>
        Lines.OfType<TLine>().ToArray();

    /// <summary>
    /// Gets records of a specific journal-line subtype filtered by a predicate.
    /// </summary>
    /// <typeparam name="TLine">The requested journal-line subtype.</typeparam>
    /// <param name="predicate">A filter predicate applied after type filtering.</param>
    /// <returns>A snapshot of matching records in encounter order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<TLine> GetLines<TLine>(Func<TLine, bool> predicate) where TLine : JournalLine
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Lines.OfType<TLine>()
            .Where(predicate)
            .ToArray();
    }

    /// <summary>
    /// Gets all records in a specific block.
    /// </summary>
    /// <param name="block">The block number to query.</param>
    /// <returns>A snapshot of records whose <see cref="JournalLine.Block"/> equals <paramref name="block"/>.</returns>
    public IReadOnlyList<JournalLine> GetLinesByBlock(int block) =>
        Lines.Where(x => x.Block == block).ToArray();

    /// <summary>
    /// Gets all records in any of the specified blocks.
    /// </summary>
    /// <param name="blocks">The block numbers to query.</param>
    /// <returns>A snapshot of matching records in encounter order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="blocks"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<JournalLine> GetLinesByBlocks(IEnumerable<int> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var blockSet = blocks.ToHashSet();
        return Lines.Where(x => blockSet.Contains(x.Block)).ToArray();
    }

    /// <summary>
    /// Gets all records whose blocks have timestamps within the provided time window.
    /// </summary>
    /// <param name="fromDateTime">Lower bound timestamp (exclusive), or <see langword="null"/>.</param>
    /// <param name="toDateTime">Upper bound timestamp (exclusive), or <see langword="null"/>.</param>
    /// <returns>
    /// A snapshot of records in matching blocks. Returns an empty snapshot when both
    /// bounds are <see langword="null"/>.
    /// </returns>
    /// <remarks>
    /// Bounds are strict: records are matched only when timestamp &gt; from and timestamp &lt; to.
    /// </remarks>
    public IReadOnlyList<JournalLine> GetLinesByDateTime(DateTime? fromDateTime, DateTime? toDateTime)
    {
        if (!fromDateTime.HasValue && !toDateTime.HasValue)
        {
            return Array.Empty<JournalLine>();
        }

        var matchingBlocks = GetLines<JournalTimeStampLine>()
            .Where(x => x.DateTime.HasValue)
            .Where(x => !fromDateTime.HasValue || x.DateTime!.Value > fromDateTime.Value)
            .Where(x => !toDateTime.HasValue || x.DateTime!.Value < toDateTime.Value)
            .Select(x => x.Block)
            .ToHashSet();

        return GetLinesByBlocks(matchingBlocks);
    }

    /// <summary>
    /// Gets all records for a specific semantic line kind.
    /// </summary>
    /// <param name="kind">The line kind to match.</param>
    /// <returns>A snapshot of matching records in encounter order.</returns>
    public IReadOnlyList<JournalLine> GetLinesByKind(JournalLineKind kind) =>
        Lines.Where(line => line.Kind == kind).ToArray();

    /// <summary>
    /// Gets all records matching any of the specified runtime types.
    /// </summary>
    /// <param name="types">Concrete journal-line types to include.</param>
    /// <returns>A snapshot of matching records in encounter order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="types"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// At least one type is <see langword="null"/> or does not derive from <see cref="JournalLine"/>.
    /// </exception>
    public IReadOnlyList<JournalLine> GetLinesByTypes(params Type[] types)
    {
        ArgumentNullException.ThrowIfNull(types);

        if (types.Any(type => type is null || !typeof(JournalLine).IsAssignableFrom(type)))
        {
            throw new ArgumentException("Every type must derive from JournalLine.", nameof(types));
        }

        return Lines
            .Where(line => types.Any(type => type.IsInstanceOfType(line)))
            .ToArray();
    }

    /// <summary>
    /// Gets whether the journal records the start of journal playback.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool IsInPlaybackMode =>
        _isInPlaybackMode ??= Lines.OfType<JournalTimeStampLine>().Any(line =>
            line.Description?.StartsWith(
                "started journal file playback",
                StringComparison.Ordinal) == true);

    /// <summary>
    /// Gets loaded add-in assemblies inferred from API messages.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public IReadOnlyList<LoadedAssembly> LoadedAssemblies =>
        _loadedAssemblies ??= LoadedAssemblyAnalyzer.GetLoadedAssemblies(this);

    /// <summary>
    /// Gets loaded-model sequences inferred from commands/UI events.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public IReadOnlyList<LoadedModel> LoadedModels =>
        _loadedModels ??= LoadedModelAnalyzer.GetLoadedModels(this);

    /// <summary>
    /// Gets the maximum recorded RAM peak, computed on first access and cached.
    /// </summary>
    public int? MaxRamPeak
    {
        get
        {
            if (!_maxRamPeakInitialized)
            {
                _maxRamPeak = Lines.OfType<JournalMemoryMetricsLine>()
                    .Select(line => line.RamPeak)
                    .Max();

                _maxRamPeakInitialized = true;
            }

            return _maxRamPeak;
        }
    }

    /// <summary>
    /// Gets the maximum recorded virtual-memory peak, computed on first access and cached.
    /// </summary>
    public int? MaxVmPeak
    {
        get
        {
            if (!_maxVmPeakInitialized)
            {
                _maxVmPeak = Lines.OfType<JournalMemoryMetricsLine>()
                    .Select(line => line.VmPeak)
                    .Max();

                _maxVmPeakInitialized = true;
            }

            return _maxVmPeak;
        }
    }

    /// <summary>
    /// Gets the minimum recorded available RAM, computed on first access and cached.
    /// </summary>
    public int? MinRamAvailable
    {
        get
        {
            if (!_minRamAvailableInitialized)
            {
                _minRamAvailable = Lines.OfType<JournalMemoryMetricsLine>()
                    .Select(line => line.RamAvailable)
                    .Min();

                _minRamAvailableInitialized = true;
            }

            return _minRamAvailable;
        }
    }

    /// <summary>
    /// Gets the minimum recorded available virtual memory, computed on first access and cached.
    /// </summary>
    public int? MinVmAvailable
    {
        get
        {
            if (!_minVmAvailableInitialized)
            {
                _minVmAvailable = Lines.OfType<JournalMemoryMetricsLine>()
                    .Select(line => line.VmAvailable)
                    .Min();

                _minVmAvailableInitialized = true;
            }

            return _minVmAvailable;
        }
    }

    /// <summary>
    /// Gets recorded exceptions inferred from timestamp/API/comment lines.
    /// Analysis is performed on first access and then cached.
    /// </summary>
    public IReadOnlyList<RecordedException> RecordedExceptions =>
        _recordedExceptions ??= RecordedExceptionAnalyzer.GetRecordedExceptions(this);

    /// <summary>
    /// Gets the session duration computed from the first and last timestamp values.
    /// The value is computed on first access and then cached, including null.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> when fewer than two timestamp values are available.
    /// </remarks>
    public TimeSpan? SessionTime
    {
        get
        {
            if (!_sessionTimeInitialized)
            {
                var timestamps = Lines
                    .OfType<JournalTimeStampLine>()
                    .Where(line => line.DateTime.HasValue)
                    .Select(line => line.DateTime.GetValueOrDefault())
                    .ToArray();

                _sessionTime = timestamps.Length < 2
                    ? null
                    : timestamps[^1] - timestamps[0];

                _sessionTimeInitialized = true;
            }

            return _sessionTime;
        }
    }

    /// <summary>
    /// Gets startup time based on known startup-command markers.
    /// The value is computed on first access and then cached, including null.
    /// </summary>
    /// <remarks>
    /// Command-ID probe order mirrors the original parser behavior:
    /// ID_REVIT_MODEL_BROWSER_OPEN, ID_STARTUP_PAGE, ID_FILE_MRU_FIRST, ID_REVIT_FILE_OPEN.
    /// </remarks>
    public TimeSpan? StartupTime
    {
        get
        {
            if (!_startupTimeInitialized)
            {
                var firstTimestamp = GetDateTimeByBlock(1);
                if (!firstTimestamp.HasValue)
                {
                    _startupTime = null;
                    _startupTimeInitialized = true;
                    return _startupTime;
                }

                string[] startupCommandIds =
                [
                    "ID_REVIT_MODEL_BROWSER_OPEN", // Revit 2019 and later
                    "ID_STARTUP_PAGE",             // Revit 2018 and earlier
                    "ID_FILE_MRU_FIRST",           // DynamoAutomation playback
                    "ID_REVIT_FILE_OPEN"           // Session started by opening a file directly
                ];

                foreach (var commandId in startupCommandIds)
                {
                    var command = Lines
                        .OfType<JournalCommandLine>()
                        .FirstOrDefault(line =>
                            string.Equals(line.CommandId, commandId, StringComparison.Ordinal));

                    if (command is null)
                    {
                        continue;
                    }

                    var timestamp = GetDateTimeByBlock(command.Block);
                    _startupTime = timestamp.HasValue
                        ? timestamp.Value - firstTimestamp.Value
                        : null;

                    _startupTimeInitialized = true;
                    return _startupTime;
                }

                _startupTime = null;
                _startupTimeInitialized = true;
            }

            return _startupTime;
        }
    }

    /// <summary>
    /// Returns a filtered text export of parsed records suitable for playback-focused scenarios.
    /// </summary>
    /// <param name="preserveTimeStamps">
    /// When <see langword="true"/>, timestamp lines are included; otherwise they are excluded.
    /// </param>
    /// <returns>
    /// A newline-joined string containing only:
    /// AddInEvent, Command, Data, Directive, KeyboardEvent, MiscCommand, MouseEvent, UiEvent,
    /// and optionally TimeStamp.
    /// </returns>
    /// <remarks>
    /// This output is not a byte-for-byte reconstruction of the original file.
    /// Line breaks are normalized to '\n'.
    /// </remarks>
    public string StripComments(bool preserveTimeStamps)
    {
        return string.Join(
            "\n",
            Lines
                .Where(line =>
                    line is JournalAddinEventLine
                        or JournalCommandLine
                        or JournalDataLine
                        or JournalDirectiveLine
                        or JournalKeyboardEventLine
                        or JournalMiscCommandLine
                        or JournalMouseEventLine
                        or JournalUiEventLine ||
                    (preserveTimeStamps && line is JournalTimeStampLine))
                .Select(line => line.RawText));
    }

    /// <summary>
    /// Gets whether playback interruption is recorded in the journal.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool WasPlaybackInterrupted =>
        _wasPlaybackInterrupted ??= Lines.OfType<JournalTimeStampLine>().Any(line =>
            line.Description is { } description &&
            description.StartsWith("stopped at line", StringComparison.Ordinal) &&
            description.EndsWith("journal file playback", StringComparison.Ordinal));

    /// <summary>
    /// Gets whether the journal indicates proper session termination.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool WasSessionTerminatedProperly =>
        _wasSessionTerminatedProperly ??= Lines.OfType<JournalTimeStampLine>().Any(line =>
            string.Equals(
                line.Description,
                "finished recording journal file",
                StringComparison.Ordinal));

    /// <summary>
    /// Compares objects by reference so duplicate line instances can be detected reliably.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<JournalLine>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(JournalLine? x, JournalLine? y) => ReferenceEquals(x, y);

        public int GetHashCode(JournalLine obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}