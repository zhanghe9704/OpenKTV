using System;
using System.IO;
using Karaoke.Library.Configuration;

namespace Karaoke.Library.Ingestion;

public sealed class DirectoryStructureParser : IMediaPathParser
{
    public bool TryParse(MediaFileContext context, out ParsedSongMetadata metadata)
    {
        metadata = default!;

        var relativePath = Path.GetRelativePath(context.RootPath, context.FilePath);
        if (string.IsNullOrEmpty(relativePath))
        {
            return false;
        }

        var segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return false;
        }

        var artistFolder = segments[^2];
        var fileName = segments[^1];
        var title = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(artistFolder) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        // Support multiple artists separated by plus sign in folder name
        // e.g., "Artist1 + Artist2" -> "Artist1 + Artist2"
        var artist = artistFolder.Trim();

        var priority = context.RootOptions.DefaultPriority ?? context.GlobalOptions.DefaultPriority;
        var channel = context.RootOptions.DefaultChannel ?? context.GlobalOptions.DefaultChannel;

        metadata = new ParsedSongMetadata(
            NormalizeRelativePath(relativePath),
            title,
            artist,
            channel,
            priority);

        return true;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
