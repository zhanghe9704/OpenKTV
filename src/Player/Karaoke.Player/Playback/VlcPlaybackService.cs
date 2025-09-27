using System.Collections.Concurrent;
using System.Windows.Forms;
using Karaoke.Common.Models;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;

namespace Karaoke.Player.Playback;

public sealed class VlcPlaybackService : IPlaybackService, IDisposable
{
    private static readonly Action<ILogger, string, Exception?> SongQueuedLog = LoggerMessage.Define<string>(
        Microsoft.Extensions.Logging.LogLevel.Debug,
        new EventId(1, nameof(SongQueuedLog)),
        "Queued song {SongId}");

    private static readonly Action<ILogger, string, Exception?> NowPlayingLog = LoggerMessage.Define<string>(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(2, nameof(NowPlayingLog)),
        "Now playing {SongId}");

    private static readonly Action<ILogger, Exception?> QueueEmptyLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(3, nameof(QueueEmptyLog)),
        "Playback queue empty");

    private static readonly Action<ILogger, Exception?> PlayerInitializedLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(4, nameof(PlayerInitializedLog)),
        "VLC player initialized");

    private static readonly Action<ILogger, string, Exception?> PlayAsyncLog = LoggerMessage.Define<string>(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(5, nameof(PlayAsyncLog)),
        "PlayAsync called, current state: {CurrentState}");

    private static readonly Action<ILogger, string, Exception?> QueueAsyncLog = LoggerMessage.Define<string>(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(6, nameof(QueueAsyncLog)),
        "QueueAsync called for song: {SongId}");

    private static readonly Action<ILogger, Exception?> VlcInitStartLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(7, nameof(VlcInitStartLog)),
        "Starting VLC initialization");

    private static readonly Action<ILogger, Exception?> VlcCoreInitLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(8, nameof(VlcCoreInitLog)),
        "VLC Core.Initialize() completed");

    private static readonly Action<ILogger, Exception?> VlcLibCreatedLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(9, nameof(VlcLibCreatedLog)),
        "LibVLC instance created");

    private static readonly Action<ILogger, Exception?> VlcPlayerCreatedLog = LoggerMessage.Define(
        Microsoft.Extensions.Logging.LogLevel.Information,
        new EventId(10, nameof(VlcPlayerCreatedLog)),
        "MediaPlayer instance created");

    private readonly ConcurrentQueue<SongDto> _queue = new();
    private readonly ILogger<VlcPlaybackService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private SongDto? _currentSong;
    private Media? _currentMedia;
    private PlaybackState _currentState = PlaybackState.Stopped;
    private bool _disposed;
    private bool _vlcInitialized;
    private Form? _playbackForm;

    public event EventHandler<SongDto>? SongChanged;
    public event EventHandler<PlaybackState>? StateChanged;

    public VlcPlaybackService(ILogger<VlcPlaybackService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        
        // Log immediately with multiple methods to ensure it's visible
        var instanceId = Guid.NewGuid().ToString("N")[..8];
        Console.WriteLine($"VlcPlaybackService constructor called - Instance ID: {instanceId}");
        System.Diagnostics.Debug.WriteLine($"VlcPlaybackService constructor called - Instance ID: {instanceId}");
        _logger.LogInformation("VlcPlaybackService constructor called - Instance ID: {InstanceId}", instanceId);
        
        try
        {
            InitializeVlc();
            Console.WriteLine("VlcPlaybackService constructor completed successfully");
            _logger.LogInformation("VlcPlaybackService constructor completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"VlcPlaybackService constructor failed: {ex.Message}");
            _logger.LogError(ex, "VlcPlaybackService constructor failed");
            _vlcInitialized = false;
        }
    }

    public async Task QueueAsync(SongDto song, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        QueueAsyncLog(_logger, song.Id, null);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _queue.Enqueue(song);
            SongQueuedLog(_logger, song.Id, null);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveFromQueueAsync(SongDto song, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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

            if (removed)
            {
                _logger.LogInformation("Removed song {SongId} from queue", song.Id);
            }
            else
            {
                _logger.LogInformation("Song {SongId} not found in queue", song.Id);
            }

            return removed;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ClearQueueAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Clear the queue
            while (_queue.TryDequeue(out _))
            {
                // Empty the queue
            }
            
            _logger.LogInformation("Queue cleared");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> CancelCurrentSongAsync(SongDto song, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Check if the specified song is the current song
            if (_currentSong != null && _currentSong.Id == song.Id)
            {
                _logger.LogInformation("Canceling current song {SongId}", song.Id);
                
                // Clear current song and move to next
                _currentSong = null;
                
                // If currently playing, move to next song
                if (_currentState == PlaybackState.Playing)
                {
                    await MoveNextInternalAsync(cancellationToken).ConfigureAwait(false);
                }
                
                return true;
            }
            
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SongDto?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _currentSong;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<SongDto?> MoveNextAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await MoveNextInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<SongDto?> MoveNextInternalAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MoveNextInternalAsync: Starting...");
        
        if (_queue.TryDequeue(out var nextSong))
        {
            _logger.LogInformation("MoveNextInternalAsync: Found song {SongId}, setting as current", nextSong.Id);
            _currentSong = nextSong;
            NowPlayingLog(_logger, nextSong.Id, null);
            SongChanged?.Invoke(this, nextSong);
            
            _logger.LogInformation("MoveNextInternalAsync: Calling PlayCurrentSongAsync...");
            await PlayCurrentSongAsync().ConfigureAwait(false);
            _logger.LogInformation("MoveNextInternalAsync: PlayCurrentSongAsync completed");
            return nextSong;
        }
        else
        {
            _logger.LogInformation("MoveNextInternalAsync: Queue empty, no more songs");
            _currentSong = null;
            QueueEmptyLog(_logger, null);
            // Don't set to Stopped - let the MediaPlayer naturally finish
            // This prevents VLC from tearing down the window
            return null;
        }
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        PlayAsyncLog(_logger, _currentState.ToString(), null);

        _logger.LogInformation("PlayAsync: VLC initialized = {VlcInitialized}", _vlcInitialized);
        _logger.LogInformation("PlayAsync: MediaPlayer null = {MediaPlayerNull}", _mediaPlayer == null);

        if (!_vlcInitialized)
        {
            _logger.LogWarning("VLC not initialized - cannot play");
            return;
        }

        _logger.LogInformation("PlayAsync: Acquiring semaphore...");
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("PlayAsync: Semaphore acquired, checking MediaPlayer...");
            if (_mediaPlayer == null)
            {
                _logger.LogWarning("MediaPlayer is null in PlayAsync");
                return;
            }

            _logger.LogInformation("PlayAsync: Current state = {CurrentState}", _currentState);
            if (_currentState == PlaybackState.Paused)
            {
                _logger.LogInformation("PlayAsync: Resuming from pause...");
                _mediaPlayer.Play();
                await SetStateAsync(PlaybackState.Playing).ConfigureAwait(false);
            }
            else if (_currentState == PlaybackState.Stopped)
            {
                _logger.LogInformation("PlayAsync: Starting from stopped, calling MoveNextAsync...");
                // Start playing from queue - call internal version to avoid semaphore deadlock
                await MoveNextInternalAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("PlayAsync: Current state {CurrentState} - no action taken", _currentState);
            }
        }
        finally
        {
            _logger.LogInformation("PlayAsync: Releasing semaphore...");
            _semaphore.Release();
        }
        _logger.LogInformation("PlayAsync: Completed");
    }

    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_vlcInitialized)
        {
            _logger.LogWarning("VLC not initialized - cannot pause");
            return;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mediaPlayer != null && _currentState == PlaybackState.Playing)
            {
                _mediaPlayer.Pause();
                await SetStateAsync(PlaybackState.Paused).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_vlcInitialized)
        {
            _logger.LogWarning("VLC not initialized - cannot stop");
            return;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                await SetStateAsync(PlaybackState.Stopped).ConfigureAwait(false);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PlaybackState> GetStateAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _currentState;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ShowPlayerWindowAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // LibVLC will show its default player window when media starts playing
            // We don't need to explicitly create a window
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task HidePlayerWindowAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Stop playback to hide the window
            await StopAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void InitializeVlc()
    {
        try
        {
            _logger.LogInformation("Starting VLC initialization...");
            
            // Set VLC library path to the bundled runtime
            var vlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
            _logger.LogInformation("VLC path: {VlcPath}", vlcPath);
            _logger.LogInformation("Path exists: {PathExists}", Directory.Exists(vlcPath));
            
            if (Directory.Exists(vlcPath))
            {
                var files = Directory.GetFiles(vlcPath, "*.dll");
                _logger.LogInformation("Found {FileCount} DLL files in VLC path", files.Length);
                foreach (var file in files)
                {
                    _logger.LogInformation("VLC file: {FileName}", Path.GetFileName(file));
                }
            }
            
            _logger.LogInformation("Calling Core.Initialize...");
            Core.Initialize(vlcPath);
            _logger.LogInformation("Core.Initialize completed");
            
            _logger.LogInformation("Creating LibVLC instance...");
            _libVlc = new LibVLC();
            _logger.LogInformation("LibVLC instance created");
            
            _logger.LogInformation("Creating MediaPlayer instance...");
            _mediaPlayer = new MediaPlayer(_libVlc);
            _logger.LogInformation("MediaPlayer instance created");
            
            // Create stable playback window
            _logger.LogInformation("Creating playback window...");
            _playbackForm = new Form
            {
                Text = "Karaoke Player",
                FormBorderStyle = FormBorderStyle.Sizable,
                StartPosition = FormStartPosition.CenterScreen,
                Size = new System.Drawing.Size(800, 600),
                BackColor = System.Drawing.Color.Black
            };
            _playbackForm.Show();
            _logger.LogInformation("Playback window created with handle: {Handle}", _playbackForm.Handle);
            
            // Pin VLC rendering to our stable window
            _logger.LogInformation("Setting VLC render target...");
            _mediaPlayer.Hwnd = _playbackForm.Handle;
            _logger.LogInformation("VLC render target set");
            
            // Disable VLC's own input handling since we have our own window
            _mediaPlayer.EnableKeyInput = false;
            _mediaPlayer.EnableMouseInput = false;
            
            // Subscribe to events
            _mediaPlayer.EndReached += OnMediaPlayerEndReached;
            _mediaPlayer.Playing += OnMediaPlayerPlaying;
            _mediaPlayer.Paused += OnMediaPlayerPaused;
            _mediaPlayer.Stopped += OnMediaPlayerStopped;
            
            _vlcInitialized = true;
            _logger.LogInformation("VLC player initialized successfully with stable window");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize VLC player - falling back to safe mode");
            _vlcInitialized = false;
        }
    }

    private async void OnMediaPlayerEndReached(object? sender, EventArgs e)
    {
        try
        {
            // Use Task.Run to avoid blocking VLC's event thread
            await Task.Run(async () =>
            {
                // Call internal version directly to avoid semaphore deadlock
                // and keep MediaPlayer in continuous playback state
                await MoveNextInternalAsync(CancellationToken.None).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling media end reached");
        }
    }

    private async void OnMediaPlayerPlaying(object? sender, EventArgs e)
    {
        await Task.Run(async () =>
        {
            await SetStateAsync(PlaybackState.Playing).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async void OnMediaPlayerPaused(object? sender, EventArgs e)
    {
        await Task.Run(async () =>
        {
            await SetStateAsync(PlaybackState.Paused).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async void OnMediaPlayerStopped(object? sender, EventArgs e)
    {
        await Task.Run(async () =>
        {
            await SetStateAsync(PlaybackState.Stopped).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    private async Task PlayCurrentSongAsync()
    {
        if (!_vlcInitialized || _mediaPlayer == null || _currentSong == null)
        {
            _logger.LogWarning("Cannot play song - VLC not initialized or missing components");
            return;
        }

        try
        {
            // Create new media
            var newMedia = new Media(_libVlc!, _currentSong.MediaPath, FromType.FromPath);
            
            // Set media to player BEFORE disposing old media
            _mediaPlayer.Media = newMedia;
            _mediaPlayer.Play();  // Play without parameters to reuse existing window
            
            // Only dispose old media AFTER new one is set and playing
            var oldMedia = _currentMedia;
            _currentMedia = newMedia;
            oldMedia?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play song {SongPath}", _currentSong.MediaPath);
            // Try to move to next song if current one fails
            await MoveNextAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task SetStateAsync(PlaybackState newState)
    {
        if (_currentState != newState)
        {
            _currentState = newState;
            StateChanged?.Invoke(this, newState);
        }
        await Task.CompletedTask;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VlcPlaybackService));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.EndReached -= OnMediaPlayerEndReached;
                _mediaPlayer.Playing -= OnMediaPlayerPlaying;
                _mediaPlayer.Paused -= OnMediaPlayerPaused;
                _mediaPlayer.Stopped -= OnMediaPlayerStopped;
                
                _mediaPlayer.Stop();
                _mediaPlayer.Dispose();
            }

            _currentMedia?.Dispose();
            _libVlc?.Dispose();
            _playbackForm?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing VLC player");
        }
        finally
        {
            _semaphore.Dispose();
        }
    }
}