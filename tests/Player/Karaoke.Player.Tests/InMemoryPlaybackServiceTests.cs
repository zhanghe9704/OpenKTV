using Karaoke.Common.Models;
using Karaoke.Player.Playback;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Karaoke.Player.Tests;

public class InMemoryPlaybackServiceTests
{
    private static SongDto CreateSong(string id)
    {
        return new SongDto(id, $"Title{id}", $"Artist{id}", $"assets/Artist{id}/Title{id}.mp3", "Stereo", 2);
    }

    [Fact]
    public async Task QueueAndMoveNextCyclesThroughSongs()
    {
        var service = new InMemoryPlaybackService(NullLogger<InMemoryPlaybackService>.Instance);
        await service.QueueAsync(CreateSong("1"), CancellationToken.None);
        await service.QueueAsync(CreateSong("2"), CancellationToken.None);

        var first = await service.MoveNextAsync(CancellationToken.None);
        var second = await service.MoveNextAsync(CancellationToken.None);
        var third = await service.MoveNextAsync(CancellationToken.None);

        Assert.Equal("1", first?.Id);
        Assert.Equal("2", second?.Id);
        Assert.Null(third);
    }
}
