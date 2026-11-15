using System;

/// <summary>
/// Represents one linked-model load derived from session events.
/// </summary>
public sealed class LoadedLink
{
    /// <summary>
    /// Initializes a loaded-link record.
    /// </summary>
    /// <param name="loadStart">Timestamp of the <c>&gt;OpenLink</c> marker.</param>
    /// <param name="fullPath">Full path of the linked model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fullPath"/> is <see langword="null"/>.</exception>
    public LoadedLink(DateTime loadStart, string fullPath)
    {
        ArgumentNullException.ThrowIfNull(fullPath);

        LoadStart = loadStart;
        FullPath = fullPath;
    }

    /// <summary>
    /// Gets the linked model file name (last segment of <see cref="FullPath"/>).
    /// </summary>
    /// <remarks>
    /// Splits on backslashes explicitly because slog paths are Windows paths
    /// regardless of the platform parsing them.
    /// </remarks>
    public string FileName => FullPath.Split('\\')[^1];

    /// <summary>
    /// Gets the full path of the linked model.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the load duration, when an end marker was recorded.
    /// </summary>
    public TimeSpan? LoadDuration => LoadEnd - LoadStart;

    /// <summary>
    /// Gets the timestamp of the <c>&lt;OpenLink</c> marker, when recorded.
    /// </summary>
    public DateTime? LoadEnd { get; internal set; }

    /// <summary>
    /// Gets the timestamp of the <c>&gt;OpenLink</c> marker.
    /// </summary>
    public DateTime LoadStart { get; }

    public override string ToString() => nameof(LoadedLink);
}