using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Karaoke.Library.Ingestion;

public sealed class KeywordFileNameParser : IMediaPathParser
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

        // Get the format specification from the root options or global options
        var format = context.RootOptions.KeywordFormat ?? context.GlobalOptions.KeywordFormat;
        if (string.IsNullOrWhiteSpace(format))
        {
            return false;
        }

        if (!TryParseKeywordFormat(fileName, format, out var parsedData))
        {
            return false;
        }

        var title = parsedData.GetValueOrDefault("SONG", string.Empty);
        var artist = parsedData.GetValueOrDefault("ARTIST", string.Empty);
        var comment = parsedData.GetValueOrDefault("COMMENT", string.Empty);
        var language = parsedData.GetValueOrDefault("LANGUAGE", string.Empty);
        var genre = parsedData.GetValueOrDefault("GENRE", string.Empty);

        // Validate that we have at least title and artist
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(artist))
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
            priority,
            string.IsNullOrWhiteSpace(language) ? null : language,
            string.IsNullOrWhiteSpace(genre) ? null : genre,
            string.IsNullOrWhiteSpace(comment) ? null : comment);

        return true;
    }

    private static bool TryParseKeywordFormat(string fileName, string format, out Dictionary<string, string> result)
    {
        result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Split the format by '-' to get the keyword sequence
        var formatKeywords = format.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim().ToUpperInvariant())
            .ToArray();

        if (formatKeywords.Length == 0)
        {
            return false;
        }

        // Split the filename by '-' to get the values
        var filenameParts = fileName.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        if (filenameParts.Length != formatKeywords.Length)
        {
            return false;
        }

        // Map keywords to values
        for (int i = 0; i < formatKeywords.Length; i++)
        {
            var keyword = formatKeywords[i];
            var value = filenameParts[i];

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            // If the keyword already exists, concatenate with ", "
            if (result.TryGetValue(keyword, out var existingValue))
            {
                result[keyword] = $"{existingValue}, {value}";
            }
            else
            {
                result[keyword] = value;
            }
        }

        return true;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}