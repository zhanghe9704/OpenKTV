using Karaoke.Common.Models;

namespace Karaoke.Player.Playback;

public interface IPlaybackService
{
    Task QueueAsync(SongDto song, CancellationToken cancellationToken);

    Task<SongDto?> GetCurrentAsync(CancellationToken cancellationToken);

    Task<SongDto?> MoveNextAsync(CancellationToken cancellationToken);
}
