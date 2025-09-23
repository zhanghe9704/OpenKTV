using System;
using System.IO;

namespace Karaoke.Library.Ingestion;

public sealed class HyphenFileNameParser : IMediaPathParser
{
    public bool TryParse(MediaFileContext context, out ParsedSongMetadata metadata)
    {
        metadata = default!;

        var relativePath = Path.GetRelativePath(context.RootPath, context.FilePath);
        if (string.IsNullOrEmpty(relativePath))
        {
            return false;
        }

        var fileName = Path.GetFileNameWithoutExtension(context.FilePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var hyphenIndex = fileName.IndexOf('-', StringComparison.Ordinal);
        if (hyphenIndex <= 0 || hyphenIndex >= fileName.Length - 1)
        {
            return false;
        }

        var artist = fileName[..hyphenIndex].Trim();
        var title = fileName[(hyphenIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

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
