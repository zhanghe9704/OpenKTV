using Karaoke.Common.Models;

namespace Karaoke.Player.Playback;

public enum PlaybackState
{
    Stopped,
    Playing,
    Paused
}

public interface IPlaybackService
{
    Task QueueAsync(SongDto song, CancellationToken cancellationToken);

    Task<bool> RemoveFromQueueAsync(SongDto song, CancellationToken cancellationToken);

    Task ClearQueueAsync(CancellationToken cancellationToken);

    Task<bool> CancelCurrentSongAsync(SongDto song, CancellationToken cancellationToken);

    Task<SongDto?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SongDto?> MoveNextAsync(CancellationToken cancellationToken);

    Task PlayAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);

    Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken);

    Task ShowPlayerWindowAsync(CancellationToken cancellationToken);

    Task HidePlayerWindowAsync(CancellationToken cancellationToken);

    event EventHandler<SongDto>? SongChanged;

    event EventHandler<PlaybackState>? StateChanged;
}
