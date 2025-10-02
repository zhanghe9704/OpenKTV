using Karaoke.Common.Models;
using Karaoke.Player.Playback;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Karaoke.Player.Tests;

public class VlcPlaybackServiceTests
{
    private readonly ILogger<VlcPlaybackService> _logger;

    public VlcPlaybackServiceTests()
    {
        // Create a logger factory for testing
        using var factory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = factory.CreateLogger<VlcPlaybackService>();
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        using var service = new VlcPlaybackService(_logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task QueueAsync_ShouldAcceptValidSong()
    {
        // Arrange
        using var service = new VlcPlaybackService(_logger);
        var song = new SongDto(
            "test-id",
            "Test Song",
            "Test Artist",
            "test-path.mp3",
            "Stereo",
            1,
            Instrumental: 0
        );

        // Act & Assert
        await service.QueueAsync(song, CancellationToken.None);
        // Should complete without throwing
        Assert.True(true);
    }

    [Theory]
    [InlineData(0)] // Left channel (instrumental)
    [InlineData(1)] // Right channel (vocals)
    [InlineData(2)] // Both channels (stereo)
    public async Task SongDto_InstrumentalValues_ShouldBeHandledCorrectly(int instrumental)
    {
        // Arrange
        using var service = new VlcPlaybackService(_logger);
        var song = new SongDto(
            $"test-id-{instrumental}",
            "Test Song",
            "Test Artist",
            "test-path.mp3",
            "Stereo",
            1,
            Instrumental: instrumental
        );

        // Act
        await service.QueueAsync(song, CancellationToken.None);
        var currentSong = await service.GetCurrentAsync(CancellationToken.None);

        // Assert
        Assert.Null(currentSong); // No song should be current until playback starts
    }

    [Fact]
    public async Task GetStateAsync_InitialState_ShouldBeStopped()
    {
        // Arrange
        using var service = new VlcPlaybackService(_logger);

        // Act
        var state = await service.GetStateAsync(CancellationToken.None);

        // Assert
        Assert.Equal(PlaybackState.Stopped, state);
    }
}