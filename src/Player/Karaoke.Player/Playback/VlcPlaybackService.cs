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
    private bool _isFullScreen;
    private FormWindowState _originalWindowState;
    private FormBorderStyle _originalBorderStyle;
    private System.Drawing.Size _originalSize;
    private System.Drawing.Point _originalLocation;

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

    public async Task MoveInQueueAsync(SongDto song, int newPosition, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(song);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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
                
                _logger.LogInformation("Moved song {SongId} from position {OldPosition} to position {NewPosition}", 
                    song.Id, songIndex, targetPosition);
            }
            else
            {
                _logger.LogWarning("Song {SongId} not found in queue for move operation", song.Id);
            }

            // Re-enqueue all songs in new order
            foreach (var remainingSong in queueList)
            {
                _queue.Enqueue(remainingSong);
            }
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

    public async Task RestartCurrentSongAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_vlcInitialized)
        {
            _logger.LogWarning("VLC not initialized - cannot restart current song");
            return;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_mediaPlayer != null && _currentSong != null)
            {
                _logger.LogInformation("Restarting current song {SongId}", _currentSong.Id);
                
                // Set position to beginning and start playing
                _mediaPlayer.Position = 0.0f;
                _mediaPlayer.Play();
                await SetStateAsync(PlaybackState.Playing).ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("No current song to restart");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ToggleFullScreenAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (_playbackForm == null)
        {
            _logger.LogWarning("No playback window to toggle full screen");
            return;
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Marshal to UI thread if needed
            if (_playbackForm.InvokeRequired)
            {
                _playbackForm.Invoke(new Action(() => ToggleFullScreenInternal()));
            }
            else
            {
                ToggleFullScreenInternal();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void ToggleFullScreenInternal()
    {
        if (_playbackForm == null) return;

        if (!_isFullScreen)
        {
            // Save current window state
            _originalWindowState = _playbackForm.WindowState;
            _originalBorderStyle = _playbackForm.FormBorderStyle;
            _originalSize = _playbackForm.Size;
            _originalLocation = _playbackForm.Location;

            // Switch to full screen on the screen where the window is currently located
            _playbackForm.WindowState = FormWindowState.Normal;
            _playbackForm.FormBorderStyle = FormBorderStyle.None;
            
            // Get the screen that contains the current window
            var currentScreen = Screen.FromControl(_playbackForm);
            _playbackForm.Bounds = currentScreen.Bounds;
            _playbackForm.TopMost = true; // Always on top when full screen

            _isFullScreen = true;
            _logger.LogInformation("Player window switched to full screen");
        }
        else
        {
            // Restore original window state
            _playbackForm.FormBorderStyle = _originalBorderStyle;
            _playbackForm.WindowState = _originalWindowState;
            _playbackForm.Size = _originalSize;
            _playbackForm.Location = _originalLocation;
            
            // Restore TopMost based on current playback state
            UpdateWindowTopMost(_currentState);

            _isFullScreen = false;
            _logger.LogInformation("Player window restored from full screen");
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
                BackColor = System.Drawing.Color.Black,
                TopMost = false,  // Will be set to true when playing
                ControlBox = false  // Remove minimize, maximize, and close buttons
            };
            
            // Add ESC key handler for exiting full screen
            _playbackForm.KeyPreview = true;
            _playbackForm.KeyDown += OnPlaybackFormKeyDown;
            
            _playbackForm.Show();
            
            // Initialize original window state for full screen toggle
            _originalWindowState = _playbackForm.WindowState;
            _originalBorderStyle = _playbackForm.FormBorderStyle;
            _originalSize = _playbackForm.Size;
            _originalLocation = _playbackForm.Location;
            _isFullScreen = false;
            
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

            // Log detailed song information
            _logger.LogInformation("=== Playing Song ===");
            _logger.LogInformation("Song ID: {SongId}", _currentSong.Id);
            _logger.LogInformation("Media Path: {MediaPath}", _currentSong.MediaPath);
            _logger.LogInformation("Instrumental Value: {Instrumental}", _currentSong.Instrumental);
            _logger.LogInformation("Channel Configuration: {ChannelConfig}", _currentSong.ChannelConfiguration);

            // Configure audiochannel mode per-media based on Instrumental value
            var instrumental = _currentSong.Instrumental;

            // Enable the audiochannel filter for this media
            newMedia.AddOption(":audio-filter=audiochannel");

            if (instrumental == 0)
            {
                // Left channel only: mode 1 or mode 4
                newMedia.AddOption(":audiochannel-mode=1"); // 1 = left channel
                _logger.LogInformation("Configured for left channel (Instrumental=0, mode=1)");
            }
            else if (instrumental == 1)
            {
                // Right channel only: mode 2 or mode 5
                newMedia.AddOption(":audiochannel-mode=2"); // 2 = right channel
                _logger.LogInformation("Configured for right channel (Instrumental=1, mode=2)");
            }
            else
            {
                // Both channels (stereo)
                newMedia.AddOption(":audiochannel-mode=0"); // 0 = stereo (both channels)
                _logger.LogInformation("Configured for stereo (Instrumental={Instrumental}, mode=0)", instrumental);
            }

            _logger.LogInformation("Added audio-filter=audiochannel to media options");

            // Set media to player BEFORE disposing old media
            _mediaPlayer.Media = newMedia;
            _mediaPlayer.Play();  // Play without parameters to reuse existing window

            // Wait a moment for VLC to start parsing the media
            await Task.Delay(500).ConfigureAwait(false);

            // Apply instrumental audio track selection (for multi-track files)
            ApplyInstrumentalAudioSelection();

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

    private void ApplyInstrumentalAudioSelection()
    {
        if (_mediaPlayer == null || _currentSong == null)
        {
            return;
        }

        try
        {
            var instrumental = _currentSong.Instrumental;
            _logger.LogInformation("=== Audio Track Selection ===");
            _logger.LogInformation("Applying instrumental audio track selection: {Instrumental}", instrumental);

            // Get audio track count and details
            var audioTrackCount = _mediaPlayer.AudioTrackCount;
            _logger.LogInformation("Audio track count: {TrackCount}", audioTrackCount);

            // Log all available audio tracks
            var trackDescriptions = _mediaPlayer.AudioTrackDescription;
            if (trackDescriptions != null)
            {
                _logger.LogInformation("Total track descriptions: {Count}", trackDescriptions.Length);
                for (int i = 0; i < trackDescriptions.Length; i++)
                {
                    var track = trackDescriptions[i];
                    _logger.LogInformation("Track [{Index}]: Id={Id}, Name={Name}",
                        i, track.Id, track.Name);
                }
            }
            else
            {
                _logger.LogWarning("AudioTrackDescription is null");
            }

            // Get current audio track
            var currentTrack = _mediaPlayer.AudioTrack;
            _logger.LogInformation("Current audio track ID: {CurrentTrackId}", currentTrack);

            // Check if we have multiple valid audio tracks (for files with separate instrumental tracks)
            var validTracks = trackDescriptions != null
                ? trackDescriptions.Where(t => t.Id >= 0).ToArray()
                : Array.Empty<LibVLCSharp.Shared.Structures.TrackDescription>();
            _logger.LogInformation("Valid tracks (Id >= 0): {Count}", validTracks.Length);

            if (validTracks.Length >= 2)
            {
                // Multi-track file: select track based on Instrumental value
                var targetTrack = instrumental == 0 ? validTracks[0] : validTracks[1];
                _logger.LogInformation("Target track for Instrumental={Instrumental}: Id={TrackId}, Name={Name}",
                    instrumental, targetTrack.Id, targetTrack.Name);

                var result = _mediaPlayer.SetAudioTrack(targetTrack.Id);
                _logger.LogInformation("SetAudioTrack result: {Result}", result);

                // Verify the track was set
                var newCurrentTrack = _mediaPlayer.AudioTrack;
                _logger.LogInformation("Audio track after SetAudioTrack: {NewTrackId}", newCurrentTrack);

                if (newCurrentTrack == targetTrack.Id)
                {
                    _logger.LogInformation("Successfully selected audio track {TrackId}", targetTrack.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to set audio track - expected {ExpectedId}, got {ActualId}",
                        targetTrack.Id, newCurrentTrack);
                }
            }
            else
            {
                // Single-track (stereo) file: need to extract specific channel
                _logger.LogInformation("Single audio track detected - applying channel selection");

                // VLC's channel selection: use SetAudioOutput or audio device selection
                // Try to set stereo mode - note this might not work for all VLC builds
                try
                {
                    // Get available audio output devices
                    var audioOutputDevices = _mediaPlayer.AudioOutputDeviceEnum;
                    if (audioOutputDevices != null)
                    {
                        _logger.LogInformation("Available audio devices: {Count}", audioOutputDevices.Count());
                        foreach (var device in audioOutputDevices)
                        {
                            _logger.LogInformation("Audio device: {DeviceId}, Description: {Description}",
                                device.DeviceIdentifier, device.Description);
                        }
                    }

                    // For stereo channel selection, we need to use audio remapping
                    // VLC doesn't provide a simple API for this, so we'll log a warning
                    _logger.LogWarning("Stereo channel selection (Instrumental={Instrumental}) requires audio remapping which is not fully supported via LibVLCSharp API",
                        instrumental);
                    _logger.LogWarning("Desired channel: {Channel}", instrumental == 0 ? "left" : "right");
                    _logger.LogWarning("Consider using the ChannelConfiguration metadata or pre-processing audio files");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error attempting to configure audio output");
                }
            }

            _logger.LogInformation("=== End Audio Track Selection ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply instrumental audio selection");
        }
    }

    private async Task SetStateAsync(PlaybackState newState)
    {
        if (_currentState != newState)
        {
            _currentState = newState;
            UpdateWindowTopMost(newState);
            StateChanged?.Invoke(this, newState);
        }
        await Task.CompletedTask;
    }

    private void UpdateWindowTopMost(PlaybackState state)
    {
        if (_playbackForm != null)
        {
            var shouldBeTopMost = state == PlaybackState.Playing;
            
            // Marshal to UI thread if needed
            if (_playbackForm.InvokeRequired)
            {
                _playbackForm.Invoke(new Action(() =>
                {
                    if (_playbackForm.TopMost != shouldBeTopMost)
                    {
                        _playbackForm.TopMost = shouldBeTopMost;
                        _logger.LogInformation("Player window TopMost set to {TopMost} (state: {State})", shouldBeTopMost, state);
                    }
                }));
            }
            else
            {
                if (_playbackForm.TopMost != shouldBeTopMost)
                {
                    _playbackForm.TopMost = shouldBeTopMost;
                    _logger.LogInformation("Player window TopMost set to {TopMost} (state: {State})", shouldBeTopMost, state);
                }
            }
        }
    }

    private async void OnPlaybackFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && _isFullScreen)
        {
            try
            {
                await ToggleFullScreenAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting full screen with ESC key");
            }
        }
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
            
            if (_playbackForm != null)
            {
                _playbackForm.KeyDown -= OnPlaybackFormKeyDown;
                _playbackForm.Dispose();
            }
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