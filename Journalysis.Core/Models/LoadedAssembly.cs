using System;
using System.Collections.Generic;
using System.IO;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents an add-in assembly inferred from API journal messages.
/// </summary>
/// <remarks>
/// Instances are built by analysis logic that correlates startup/registration-related
/// API messages. The object stores journal-reported strings as-is; no assembly loading
/// or file-system validation is performed.
/// </remarks>
public sealed class LoadedAssembly
{
    /// <summary>
    /// Initializes a loaded-assembly model from parsed API message data.
    /// </summary>
    /// <param name="name">Application name as reported in API message text.</param>
    /// <param name="className">Application class name as reported in API message text.</param>
    /// <param name="path">
    /// Assembly path as reported in journal text. This may use separators and roots
    /// that do not match the current operating system.
    /// </param>
    /// <param name="version">Assembly version text, when present in the source message.</param>
    /// <param name="vendor">Vendor name text, when present.</param>
    /// <param name="vendorDetails">Vendor details text (typically parenthesized), when present.</param>
    /// <param name="initialEvent">The API message that first established this assembly record.</param>
    public LoadedAssembly(
        string name,
        string className,
        string path,
        string? version,
        string? vendor,
        string? vendorDetails,
        JournalApiMessageLine initialEvent)
    {
        Name = name;
        ClassName = className;
        Path = path;

        // These are paths recorded by Revit, not necessarily paths on the current OS.
        var separator = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        Filename = path[(separator + 1)..];

        Version = version;
        Vendor = vendor;
        VendorDetails = vendorDetails;
        Events = [initialEvent];
    }

    /// <summary>
    /// Gets the application class name associated with the loaded assembly.
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    /// Gets the correlated API messages associated with this assembly.
    /// </summary>
    /// <remarks>
    /// The first entry is the <paramref name="initialEvent"/> passed to the constructor.
    /// Additional events may be appended by analysis correlation rules.
    /// </remarks>
    public List<JournalApiMessageLine> Events { get; }

    /// <summary>
    /// Gets the file name portion extracted from <see cref="Path"/>.
    /// </summary>
    public string Filename { get; }

    /// <summary>
    /// Gets or sets the application GUID when discovered from related API messages.
    /// </summary>
    public string? Guid { get; set; }

    /// <summary>
    /// Gets the application name associated with the loaded assembly.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the assembly path as recorded in journal text.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the vendor name when available.
    /// </summary>
    public string? Vendor { get; }

    /// <summary>
    /// Gets optional vendor detail text when available.
    /// </summary>
    public string? VendorDetails { get; }

    /// <summary>
    /// Gets the assembly version text when available.
    /// </summary>
    public string? Version { get; }
}