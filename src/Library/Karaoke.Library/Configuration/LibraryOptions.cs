using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Karaoke.Library.Configuration;

public sealed class LibraryOptions
{
    public const string SectionName = "Library";

    public int DefaultPriority { get; set; } = 2;

    public string DefaultChannel { get; set; } = "Stereo";

    public string DatabasePath { get; set; } = "data/library.db";

    [SuppressMessage("Usage", "CA2227", Justification = "Configuration binding requires a set accessor")]
    [SuppressMessage("Design", "CA1002", Justification = "List binding is acceptable for configuration data")]
    public List<string> SupportedExtensions { get; set; } = new() { ".mp3", ".wav", ".mp4", ".mkv" };

    [SuppressMessage("Usage", "CA2227", Justification = "Configuration binding requires a set accessor")]
    [SuppressMessage("Design", "CA1002", Justification = "List binding is acceptable for configuration data")]
    public List<LibraryRootOptions> Roots { get; set; } = new();

    public string? KeywordFormat { get; set; }
}

public sealed class LibraryRootOptions
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int? DefaultPriority { get; set; }

    public string? DefaultChannel { get; set; }

    public string? DriveOverride { get; set; }

    public string? KeywordFormat { get; set; }
}
