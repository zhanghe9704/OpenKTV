namespace Karaoke.Library.Storage.Models;

public sealed record SongRecord(
    string Id,
    string RootName,
    string RelativePath,
    string Title,
    string Artist,
    string ChannelConfiguration,
    int Priority,
    DateTimeOffset UpdatedAt,
    string? Language = null,
    string? Genre = null,
    string? Comment = null,
    int Instrumental = 0);
