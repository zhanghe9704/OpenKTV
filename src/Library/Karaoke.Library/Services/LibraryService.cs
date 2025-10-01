using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Karaoke.Common;
using Karaoke.Common.Models;
using Karaoke.Library.Configuration;
using Karaoke.Library.Storage;
using Karaoke.Library.Storage.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karaoke.Library.Services;

public sealed class LibraryService : ILibraryService
{
    private static readonly Action<ILogger, string, Exception?> SongUpsertedLog = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1, nameof(SongUpsertedLog)),
        "Catalog entry upserted for {SongId}");

    private readonly ILogger<LibraryService> _logger;
    private readonly ILibraryRepository _repository;
    private readonly IOptionsMonitor<LibraryOptions> _options;
    private readonly IAppEnvironment _appEnvironment;

    public LibraryService(
        ILibraryRepository repository,
        ILogger<LibraryService> logger,
        IOptionsMonitor<LibraryOptions> options,
        IAppEnvironment appEnvironment)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(appEnvironment);
        _repository = repository;
        _logger = logger;
        _options = options;
        _appEnvironment = appEnvironment;
    }

    public async Task<IReadOnlyList<SongDto>> GetAllSongsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var songs = await _repository.GetSongsAsync(cancellationToken).ConfigureAwait(false);

        var result = songs
            .Select(ToSongDto)
            .OrderBy(song => song.Priority)
            .ThenBy(song => song.Artist, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(song => song.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList()
            .AsReadOnly();

        return result;
    }

    public async Task<IReadOnlyList<SongDto>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query))
        {
            return await GetAllSongsAsync(cancellationToken).ConfigureAwait(false);
        }

        var normalized = query.Trim();

        var songs = await _repository.GetSongsAsync(cancellationToken).ConfigureAwait(false);

        var results = songs
            .Select(ToSongDto)
            .Where(song => ContainsCultureInvariant(song.Title, normalized) || ContainsCultureInvariant(song.Artist, normalized))
            .OrderBy(song => song.Priority)
            .ThenBy(song => song.Artist, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(song => song.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList()
            .AsReadOnly();

        return results;
    }

    public async Task UpsertAsync(SongDto song, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        var record = ToSongRecord(song);
        await _repository.UpsertSongAsync(record, cancellationToken).ConfigureAwait(false);
        SongUpsertedLog(_logger, song.Id, null);
    }

    private static bool ContainsCultureInvariant(string source, string value)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        return CultureInfo.CurrentCulture.CompareInfo.IndexOf(
            source,
            value,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    private SongDto ToSongDto(SongRecord record)
    {
        return new SongDto(
            record.Id,
            record.Title,
            record.Artist,
            ResolveMediaPath(record.RootName, record.RelativePath),
            record.ChannelConfiguration,
            record.Priority,
            record.Language,
            record.Genre,
            record.Comment);
    }

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
            song.Comment);
    }

    private string ResolveMediaPath(string rootName, string relativePath)
    {
        var options = _options.CurrentValue;
        var root = options.Roots?.FirstOrDefault(r => string.Equals(r.Name, rootName, StringComparison.OrdinalIgnoreCase));
        var resolvedRoot = ResolveRootPath(root?.Path, root?.DriveOverride);
        var normalizedRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(resolvedRoot, normalizedRelative));
    }

    private string ResolveRootPath(string? path, string? driveOverride)
    {
        var effectivePath = path;
        if (string.IsNullOrWhiteSpace(effectivePath))
        {
            effectivePath = _appEnvironment.AssetsRootPath;
        }

        if (!string.IsNullOrWhiteSpace(driveOverride)
            && Path.IsPathFullyQualified(effectivePath)
            && effectivePath.Length > 1)
        {
            effectivePath = driveOverride + effectivePath[1..];
        }

        if (Path.IsPathFullyQualified(effectivePath))
        {
            return effectivePath;
        }

        return Path.GetFullPath(Path.Combine(_appEnvironment.ApplicationRootPath, effectivePath));
    }
}
