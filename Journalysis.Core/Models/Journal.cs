using System;
using System.Collections.Generic;
using System.Linq;
using Journalysis.Core.Analysis;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents a parsed Revit journal file and exposes common analysis helpers.
/// </summary>
public sealed class Journal
{
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

    public IReadOnlyList<JournalLine> Lines { get; }
    public int? Version { get; }
    public string? User { get; }
    public int BlockCount { get; }
    public string? Path { get; }
    public string? Build { get; }
    public string? Branch { get; }
    public string? Release { get; }
    public string? Machine { get; }
    public string? OsVersion { get; }
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

    public bool ContainsExceptions => RecordedExceptions.Count > 0;

    /// <summary>
    /// Gets the journal date/time from block 1.
    /// The value is computed on first access and then cached,
    /// including when the result is null.
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

    public IReadOnlyList<JournalLine> GetFirstLines(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        return Lines.Take(number).ToArray();
    }

    public IReadOnlyList<JournalLine> GetLastLines(int number)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(number);
        return Lines.Skip(Math.Max(0, Lines.Count - number)).ToArray();
    }

    public IReadOnlyList<TLine> GetLines<TLine>() where TLine : JournalLine =>
        Lines.OfType<TLine>().ToArray();

    public IReadOnlyList<TLine> GetLines<TLine>(Func<TLine, bool> predicate) where TLine : JournalLine
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return Lines.OfType<TLine>()
            .Where(predicate)
            .ToArray();
    }

    public IReadOnlyList<JournalLine> GetLinesByBlock(int block) =>
        Lines.Where(x => x.Block == block).ToArray();

    public IReadOnlyList<JournalLine> GetLinesByBlocks(IEnumerable<int> blocks)
    {
        ArgumentNullException.ThrowIfNull(blocks);

        var blockSet = blocks.ToHashSet();
        return Lines.Where(x => blockSet.Contains(x.Block)).ToArray();
    }

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

    public IReadOnlyList<JournalLine> GetLinesByKind(JournalLineKind kind) =>
    Lines.Where(line => line.Kind == kind).ToArray();

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
    /// Gets whether the journal records the start of playback.
    /// The result is computed on first access and then cached.
    /// </summary>
    public bool IsInPlaybackMode =>
        _isInPlaybackMode ??= Lines.OfType<JournalTimeStampLine>().Any(line =>
            line.Description?.StartsWith(
                "started journal file playback",
                StringComparison.Ordinal) == true);

    /// <summary>
    /// Gets the loaded assemblies, analyzing the journal on first access
    /// and caching the result for subsequent accesses.
    /// </summary>
    public IReadOnlyList<LoadedAssembly> LoadedAssemblies =>
        _loadedAssemblies ??= LoadedAssemblyAnalyzer.GetLoadedAssemblies(this);

    /// <summary>
    /// Gets the loaded models, analyzing the journal on first access
    /// and caching the result for subsequent accesses.
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
    /// Gets the maximum recorded virtual memory peak, computed on first access and cached.
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
    /// Gets the recorded exceptions, analyzing the journal on first access
    /// and caching the result for subsequent accesses.
    /// </summary>
    public IReadOnlyList<RecordedException> RecordedExceptions =>
        _recordedExceptions ??= RecordedExceptionAnalyzer.GetRecordedExceptions(this);

    /// <summary>
    /// Gets the session duration computed from the first and last timestamp lines.
    /// The value is computed on first access and then cached, including null.
    /// </summary>
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
    /// Gets the startup time based on known startup command markers.
    /// The value is computed on first access and then cached, including null.
    /// </summary>
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
    /// Returns a filtered export of parsed records.
    /// This is not a byte-for-byte reconstruction of the original file.
    /// </summary>
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
    /// Gets whether the journal indicates a proper session termination.
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