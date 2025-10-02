namespace Karaoke.Common.Models;

public sealed record SongDto(
    string Id,
    string Title,
    string Artist,
    string MediaPath,
    string ChannelConfiguration,
    int Priority,
    string? Language = null,
    string? Genre = null,
    string? Comment = null,
    int Instrumental = 0,
    double LoudnessLufs = -14.0,
    double GainDb = 0.0);
