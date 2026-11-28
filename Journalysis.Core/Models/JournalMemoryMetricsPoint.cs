using System;

namespace Journalysis.Core.Models;

/// <summary>
/// One sampled memory-metric value reported for a timestamp-delimited block.
/// </summary>
/// <param name="Metric">The metric this value represents.</param>
/// <param name="Block">Block number the value is reported under; block-0 metrics are mapped to block 1.</param>
/// <param name="TimeStamp">Timestamp of the block, when available.</param>
/// <param name="MinutesSinceSessionStart">
/// Minutes from the session start timestamp to this point's block timestamp;
/// null when no timestamp or session-start timestamp is available.
/// </param>
/// <param name="Value">Metric value in MB.</param>
public sealed record JournalMemoryMetricsPoint(
    JournalMemoryMetric Metric,
    int Block,
    DateTime? TimeStamp,
    double? MinutesSinceSessionStart,
    int Value);
