using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Karaoke.Common;
using Karaoke.Common.Models;
using Karaoke.Library.Configuration;
using Karaoke.Library.Services;
using Karaoke.Library.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karaoke.Library.Ingestion;

public sealed class LibraryIngestionService : ILibraryIngestionService
{
    private static readonly Action<ILogger, string, Exception?> RootMissingLog = LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1001, nameof(RootMissingLog)),
        "Library root '{RootName}' not found on disk");

    private static readonly Action<ILogger, string, string, Exception?> FileSkippedLog = LoggerMessage.Define<string, string>(
        LogLevel.Debug,
        new EventId(1002, nameof(FileSkippedLog)),
        "Skipped media file '{File}' in root '{RootName}'");

    private readonly IEnumerable<IMediaPathParser> _parsers;
    private readonly ILibraryService _libraryService;
    private readonly LibraryOptions _options;
    private readonly IAppEnvironment _appEnvironment;
    private readonly ILogger<LibraryIngestionService> _logger;
    private readonly ILibraryRepository _repository;
    private readonly HashSet<string> _extensionSet;

    public LibraryIngestionService(
        IEnumerable<IMediaPathParser> parsers,
        ILibraryService libraryService,
        ILibraryRepository repository,
        IOptions<LibraryOptions> options,
        IAppEnvironment appEnvironment,
        ILogger<LibraryIngestionService> logger)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        ArgumentNullException.ThrowIfNull(libraryService);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(appEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        _parsers = parsers;
        _libraryService = libraryService;
        _repository = repository;
        _options = options.Value;
        _appEnvironment = appEnvironment;
        _logger = logger;
        _extensionSet = new HashSet<string>(_options.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<LibraryIngestionResult> ScanAsync(CancellationToken cancellationToken)
    {
        var processed = 0;
        var skipped = 0;

        await _repository.InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _repository.DeleteAllSongsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var root in _options.Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resolvedRoot = ResolveRootPath(root.Path, root.DriveOverride);
            if (!Directory.Exists(resolvedRoot))
            {
                RootMissingLog(_logger, root.Name, null);
                skipped++;
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(resolvedRoot, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsSupportedMedia(file))
                {
                    continue;
                }

                var context = new MediaFileContext(root.Name, resolvedRoot, root, _options, file);

                if (!TryParse(context, out var parsedMetadata))
                {
                    skipped++;
                    FileSkippedLog(_logger, file, root.Name, null);
                    continue;
                }

                var songDto = ToSongDto(root.Name, parsedMetadata);
                await _libraryService.UpsertAsync(songDto, cancellationToken).ConfigureAwait(false);
                processed++;
            }
        }

        return new LibraryIngestionResult(processed, skipped);
    }

    private static SongDto ToSongDto(string rootName, ParsedSongMetadata metadata)
    {
        var sanitizedTitle = Sanitize(metadata.Title);
        var sanitizedArtist = Sanitize(metadata.Artist);

        return new SongDto(
            CreateSongId(rootName, metadata.RelativePath),
            sanitizedTitle,
            sanitizedArtist,
            metadata.MediaPath,
            metadata.ChannelConfiguration,
            metadata.Priority);
    }

    private bool TryParse(MediaFileContext context, out ParsedSongMetadata metadata)
    {
        foreach (var parser in _parsers)
        {
            if (parser.TryParse(context, out metadata))
            {
                return true;
            }
        }

        metadata = default!;
        return false;
    }

    private bool IsSupportedMedia(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return _extensionSet.Contains(extension);
    }

    private string ResolveRootPath(string path, string? driveOverride)
    {
        var effectivePath = path;
        if (!string.IsNullOrWhiteSpace(driveOverride) && Path.IsPathFullyQualified(path) && path.Length > 1)
        {
            effectivePath = driveOverride + path[1..];
        }

        if (Path.IsPathFullyQualified(effectivePath))
        {
            return effectivePath;
        }

        return Path.GetFullPath(Path.Combine(_appEnvironment.ApplicationRootPath, effectivePath));
    }

    [SuppressMessage("Globalization", "CA1308", Justification = "Lowercase identifiers simplify comparisons across operating systems.")]
    private static string CreateSongId(string rootName, string relativePath)
    {
        return $"{rootName}:{relativePath.ToLowerInvariant()}";
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var sanitized = HtmlTagRegex.Replace(trimmed, string.Empty);
        sanitized = sanitized
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal);
        return sanitized;
    }

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
