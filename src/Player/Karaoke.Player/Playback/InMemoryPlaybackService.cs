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

    public event EventHandler<SongDto>? SongChanged;
    public event EventHandler<PlaybackState>? StateChanged;

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
}
