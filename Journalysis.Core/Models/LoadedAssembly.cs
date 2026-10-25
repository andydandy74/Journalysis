using System;
using System.Collections.Generic;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents an assembly inferred from API messages or assembly-load comments.
/// </summary>
/// <remarks>
/// Instances are built from startup API messages or explicit assembly-load comments.
/// Related API messages may be attached by analysis correlation rules.
/// No assembly loading or file-system validation is performed.
/// </remarks>
public sealed class LoadedAssembly
{
    /// <summary>
    /// Initializes a loaded-assembly model from journal-reported data.
    /// </summary>
    /// <param name="name">Application or assembly name reported by the source record.</param>
    /// <param name="className">Application class name, when available.</param>
    /// <param name="path">
    /// Assembly path as reported in journal text. This may use separators and roots
    /// that do not match the current operating system.
    /// </param>
    /// <param name="version">Assembly version text, when present in the source message.</param>
    /// <param name="vendor">Vendor name text, when present.</param>
    /// <param name="vendorDetails">Vendor details text (typically parenthesized), when present.</param>
    /// <param name="initialEvent">The journal record that established this assembly record.</param>
    public LoadedAssembly(
        string name,
        string? className,
        string path,
        string? version,
        string? vendor,
        string? vendorDetails,
        JournalLine initialEvent)
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
    /// Gets the application class name, when supplied by the source record.
    /// </summary>
    public string? ClassName { get; }

    /// <summary>
    /// Gets the journal records associated with this assembly.
    /// </summary>
    /// <remarks>
    /// The first entry is the initial event passed to the constructor.
    /// Additional events may be appended by analysis correlation rules.
    /// </remarks>
    public List<JournalLine> Events { get; }

    /// <summary>
    /// Gets the file name portion extracted from <see cref="Path"/>.
    /// </summary>
    public string Filename { get; }

    /// <summary>
    /// Gets or sets the application GUID when discovered from related API messages.
    /// </summary>
    public string? Guid { get; set; }

    /// <summary>
    /// Gets the application name from an API message or the assembly name
    /// from an assembly-load comment.
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