using System;
using System.Collections.Generic;
using System.IO;

namespace Journalysis.Core.Models;

/// <summary>
/// Represents an add-in assembly discovered through API journal messages.
/// </summary>
public sealed class LoadedAssembly
{
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

    public string Name { get; }
    public string ClassName { get; }
    public string Path { get; }
    public string Filename { get; }
    public string? Version { get; }
    public string? Vendor { get; }
    public string? VendorDetails { get; }
    public string? Guid { get; set; }
    public List<JournalApiMessageLine> Events { get; }
}