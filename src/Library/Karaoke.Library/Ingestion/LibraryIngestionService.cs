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
using Karaoke.Library.Storage.Models;
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
    private readonly IOptionsMonitor<LibraryOptions> _optionsMonitor;
    private readonly IAppEnvironment _appEnvironment;
    private readonly ILogger<LibraryIngestionService> _logger;
    private readonly ILibraryRepository _repository;
    private readonly ILoudnessAnalysisService _loudnessAnalysisService;
    private readonly HashSet<string> _extensionSet;

    public LibraryIngestionService(
        IEnumerable<IMediaPathParser> parsers,
        ILibraryRepository repository,
        IOptionsMonitor<LibraryOptions> optionsMonitor,
        IAppEnvironment appEnvironment,
        ILogger<LibraryIngestionService> logger,
        ILoudnessAnalysisService loudnessAnalysisService)
    {
        ArgumentNullException.ThrowIfNull(parsers);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(appEnvironment);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loudnessAnalysisService);

        _parsers = parsers;
        _repository = repository;
        _optionsMonitor = optionsMonitor;
        _appEnvironment = appEnvironment;
        _logger = logger;
        _loudnessAnalysisService = loudnessAnalysisService;
        _extensionSet = new HashSet<string>(optionsMonitor.CurrentValue.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<LibraryIngestionResult> ScanAsync(CancellationToken cancellationToken, IProgress<ScanProgress>? progress = null)
    {
        return await ScanSpecificRootsAsync(null, cancellationToken, progress).ConfigureAwait(false);
    }

    public async Task<LibraryIngestionResult> ScanSpecificRootsAsync(IEnumerable<string>? rootNames, CancellationToken cancellationToken, IProgress<ScanProgress>? progress = null)
    {
        var _options = _optionsMonitor.CurrentValue; // Get the latest configuration
        
        var processed = 0;
        var skipped = 0;

        await _repository.InitializeAsync(cancellationToken).ConfigureAwait(false);
        
        // If specific roots are specified, only delete songs from those roots
        if (rootNames != null)
        {
            var rootNamesToScan = rootNames.ToHashSet();
            foreach (var rootName in rootNamesToScan)
            {
                await _repository.DeleteSongsByRootAsync(rootName, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Full rescan - delete all songs
            await _repository.DeleteAllSongsAsync(cancellationToken).ConfigureAwait(false);
        }

        // Determine which roots to scan
        var rootsToScan = rootNames != null 
            ? _options.Roots.Where(r => rootNames.Contains(r.Name))
            : _options.Roots;

        foreach (var root in rootsToScan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resolvedRoot = ResolveRootPath(root.Path, root.DriveOverride);
            if (!Directory.Exists(resolvedRoot))
            {
                RootMissingLog(_logger, root.Name, null);
                skipped++;
                continue;
            }

            _logger.LogInformation("[VolumeNormalization] Root: {RootName}, VolumeNormalization enabled: {Enabled}", root.Name, root.VolumeNormalization);

            var rootFilesScanned = 0;
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

                var songDto = ToSongDto(root, resolvedRoot, parsedMetadata);

                // Perform loudness analysis if VolumeNormalization is enabled for this root
                if (root.VolumeNormalization)
                {
                    _logger.LogInformation("[VolumeNormalization] Analyzing: {FilePath}", songDto.MediaPath);
                    var loudnessResult = await _loudnessAnalysisService.AnalyzeLoudnessAsync(songDto.MediaPath, cancellationToken).ConfigureAwait(false);
                    if (loudnessResult.HasValue)
                    {
                        _logger.LogInformation("[VolumeNormalization] Result: {Loudness} LUFS, {Gain} dB gain for {FilePath}",
                            loudnessResult.Value.loudnessLufs, loudnessResult.Value.gainDb, songDto.MediaPath);
                        songDto = songDto with
                        {
                            LoudnessLufs = loudnessResult.Value.loudnessLufs,
                            GainDb = loudnessResult.Value.gainDb
                        };
                    }
                    else
                    {
                        _logger.LogWarning("[VolumeNormalization] Analysis failed for {FilePath}", songDto.MediaPath);
                    }
                }
                else
                {
                    _logger.LogDebug("[VolumeNormalization] Skipping analysis for {FilePath} (normalization disabled)", songDto.MediaPath);
                }

                var songRecord = ToSongRecord(songDto);
                await _repository.UpsertSongAsync(songRecord, cancellationToken).ConfigureAwait(false);
                processed++;
                rootFilesScanned++;

                // Report progress every 10 files or if it's a new file being scanned
                if (rootFilesScanned % 10 == 0 || rootFilesScanned == 1)
                {
                    progress?.Report(new ScanProgress(root.Name, rootFilesScanned, Path.GetFileName(file)));
                    // Yield to allow UI to process the progress update
                    await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                }
            }

            // Report final count for this root
            if (rootFilesScanned > 0)
            {
                progress?.Report(new ScanProgress(root.Name, rootFilesScanned, "Completed"));
            }
        }

        return new LibraryIngestionResult(processed, skipped);
    }

    private SongDto ToSongDto(LibraryRootOptions rootOptions, string resolvedRootPath, ParsedSongMetadata metadata)
    {
        var sanitizedTitle = Sanitize(metadata.Title);
        var sanitizedArtist = Sanitize(metadata.Artist);
        var sanitizedLanguage = metadata.Language != null ? Sanitize(metadata.Language) : null;
        var sanitizedGenre = metadata.Genre != null ? Sanitize(metadata.Genre) : null;
        var sanitizedComment = metadata.Comment != null ? Sanitize(metadata.Comment) : null;
        var mediaPath = ResolveMediaPath(resolvedRootPath, metadata.RelativePath);
        var rootName = rootOptions.Name;

        return new SongDto(
            CreateSongId(rootName, metadata.RelativePath),
            sanitizedTitle,
            sanitizedArtist,
            mediaPath,
            metadata.ChannelConfiguration,
            metadata.Priority,
            sanitizedLanguage,
            sanitizedGenre,
            sanitizedComment,
            rootOptions.Instrumental);
    }

    private static string ResolveMediaPath(string rootPath, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(rootPath, normalized));
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

    private static SongRecord ToSongRecord(SongDto song)
    {
        // Root name and relative path are encoded in the SongId as "Root:relative".
        var separatorIndex = song.Id.IndexOf(':', StringComparison.Ordinal);
        var rootName = separatorIndex > 0 ? song.Id[..separatorIndex] : string.Empty;
        var relativePath = separatorIndex > 0 ? song.Id[(separatorIndex + 1)..] : song.Id;

        return new SongRecord(
            song.Id,
            rootName,
            relativePath,
            song.Title,
            song.Artist,
            song.ChannelConfiguration,
            song.Priority,
            DateTimeOffset.UtcNow,
            song.Language,
            song.Genre,
            song.Comment,
            song.Instrumental,
            song.LoudnessLufs,
            song.GainDb);
    }
}
