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

        var artistPart = fileName[..hyphenIndex].Trim();
        var title = fileName[(hyphenIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(artistPart) || string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        // Support multiple artists separated by various separators in filename
        // Replace common separators (_, -, space, ^, VS) with + for consistency
        // e.g., "Artist1_Artist2 - Song Title" -> "Artist1 + Artist2"
        // e.g., "Artist1 VS Artist2 - Song Title" -> "Artist1 + Artist2"
        var artist = NormalizeArtistSeparators(artistPart);

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

    private static string NormalizeArtistSeparators(string artist)
    {
        // Replace common artist separators with +
        // Handle "VS" (uppercase only, no spaces) first with a placeholder to avoid double processing
        // e.g., "Artist1VSArtist2" -> "Artist1 + Artist2"
        const string placeholder = "\u0001"; // Use a control character as placeholder
        var normalized = artist.Replace("VS", placeholder);

        // Replace other separators: _, -, ^, and standalone spaces between words
        // Use regex to handle multiple consecutive separators
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            @"[\s_\-\^]+",
            " + ");

        // Replace placeholder with +
        normalized = normalized.Replace(placeholder, " + ");

        // Clean up any leading/trailing + signs and extra spaces
        normalized = normalized.Trim().Trim('+').Trim();

        // Ensure consistent spacing around +
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*\+\s*", " + ");

        return normalized;
    }
}
