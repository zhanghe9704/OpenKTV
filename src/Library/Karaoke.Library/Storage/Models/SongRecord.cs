namespace Karaoke.Library.Storage.Models;

public sealed record SongRecord(
    string Id,
    string RootName,
    string RelativePath,
    string Title,
    string Artist,
    string ChannelConfiguration,
    int Priority,
    DateTimeOffset UpdatedAt);
