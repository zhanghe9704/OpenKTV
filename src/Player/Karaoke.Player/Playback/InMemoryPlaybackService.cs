using System;
using System.Collections.Concurrent;
using Karaoke.Common.Models;
using Microsoft.Extensions.Logging;

namespace Karaoke.Player.Playback;

public sealed class InMemoryPlaybackService : IPlaybackService
{
    private static readonly Action<ILogger, string, Exception?> SongQueuedLog = LoggerMessage.Define<string>(
        LogLevel.Debug,
        new EventId(1, nameof(SongQueuedLog)),
        "Queued song {SongId}");

    private static readonly Action<ILogger, string, Exception?> NowPlayingLog = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(2, nameof(NowPlayingLog)),
        "Now playing {SongId}");

    private static readonly Action<ILogger, Exception?> QueueEmptyLog = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(3, nameof(QueueEmptyLog)),
        "Playback queue empty");

    private readonly ConcurrentQueue<SongDto> _queue = new();
    private readonly ILogger<InMemoryPlaybackService> _logger;
    private SongDto? _current;
    private PlaybackState _state = PlaybackState.Stopped;
    private int _volume = 100;

    public event EventHandler<SongDto>? SongChanged;
    public event EventHandler<PlaybackState>? StateChanged;
    public event EventHandler<int>? VolumeChanged;

    public InMemoryPlaybackService(ILogger<InMemoryPlaybackService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task QueueAsync(SongDto song, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        _queue.Enqueue(song);
        SongQueuedLog(_logger, song.Id, null);

        return Task.CompletedTask;
    }

    public Task<bool> RemoveFromQueueAsync(SongDto song, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        // Convert queue to list, remove the song, and rebuild queue
        var queueList = new List<SongDto>();
        while (_queue.TryDequeue(out var queuedSong))
        {
            queueList.Add(queuedSong);
        }

        var removed = queueList.Remove(song);
        
        // Re-enqueue remaining songs
        foreach (var remainingSong in queueList)
        {
            _queue.Enqueue(remainingSong);
        }

        return Task.FromResult(removed);
    }

    public Task ClearQueueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Clear the queue
        while (_queue.TryDequeue(out _))
        {
            // Empty the queue
        }

        return Task.CompletedTask;
    }

    public Task MoveInQueueAsync(SongDto song, int newPosition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        // Convert queue to list, move the song, and rebuild queue
        var queueList = new List<SongDto>();
        while (_queue.TryDequeue(out var queuedSong))
        {
            queueList.Add(queuedSong);
        }

        // Find the song to move
        var songIndex = queueList.FindIndex(s => s.Id == song.Id);
        if (songIndex >= 0)
        {
            // Remove song from current position
            queueList.RemoveAt(songIndex);
            
            // Insert at new position (clamp to valid range)
            var targetPosition = Math.Max(0, Math.Min(newPosition, queueList.Count));
            queueList.Insert(targetPosition, song);
        }

        // Re-enqueue all songs in new order
        foreach (var remainingSong in queueList)
        {
            _queue.Enqueue(remainingSong);
        }

        return Task.CompletedTask;
    }

    public Task<bool> CancelCurrentSongAsync(SongDto song, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        // Check if the specified song is the current song
        if (_current != null && _current.Id == song.Id)
        {
            // Clear current song
            _current = null;
            return Task.FromResult(true);
        }
        
        return Task.FromResult(false);
    }

    public Task RestartCurrentSongAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For in-memory service, we can't actually restart playback
        // but we can simulate the action for testing
        if (_current != null)
        {
            // Just ensure we're in playing state if we have a current song
            _state = PlaybackState.Playing;
            StateChanged?.Invoke(this, _state);
        }

        return Task.CompletedTask;
    }

    public Task ToggleFullScreenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For in-memory service, we can't actually toggle full screen
        // This is just a stub for testing
        return Task.CompletedTask;
    }

    public Task ToggleVocalAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // For in-memory service, we can't actually toggle vocal tracks
        // This is just a stub for testing
        return Task.CompletedTask;
    }

    public Task<SongDto?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_current);
    }

    public Task<SongDto?> MoveNextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_queue.TryDequeue(out var next))
        {
            _current = next;
            NowPlayingLog(_logger, next.Id, null);
        }
        else
        {
            _current = null;
            QueueEmptyLog(_logger, null);
        }

        return Task.FromResult(_current);
    }

    public Task PlayAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (_state == PlaybackState.Paused)
        {
            _state = PlaybackState.Playing;
            StateChanged?.Invoke(this, _state);
        }
        else if (_state == PlaybackState.Stopped && _current != null)
        {
            _state = PlaybackState.Playing;
            StateChanged?.Invoke(this, _state);
        }
        
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (_state == PlaybackState.Playing)
        {
            _state = PlaybackState.Paused;
            StateChanged?.Invoke(this, _state);
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        _state = PlaybackState.Stopped;
        StateChanged?.Invoke(this, _state);
        
        return Task.CompletedTask;
    }

    public Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_state);
    }

    public Task ShowPlayerWindowAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task HidePlayerWindowAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(int volume, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _volume = Math.Clamp(volume, 0, 100);
        VolumeChanged?.Invoke(this, _volume);

        return Task.CompletedTask;
    }

    public Task<int> GetVolumeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_volume);
    }
}
