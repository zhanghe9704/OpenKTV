namespace Karaoke.Common.Models;

public sealed record SongDto(
    string Id,
    string Title,
    string Artist,
    string MediaPath,
    string ChannelConfiguration,
    int Priority);
