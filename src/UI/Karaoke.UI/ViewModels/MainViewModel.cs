using CommunityToolkit.Mvvm.ComponentModel;
using Karaoke.Common.Models;
using Karaoke.Library.Services;
using Karaoke.Player.Playback;
using Microsoft.UI.Xaml;
using Karaoke.Library.Ingestion;

namespace Karaoke.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryIngestionService _ingestionService;

    [ObservableProperty]
    private IReadOnlyList<SongDto> _songs = Array.Empty<SongDto>();

    [ObservableProperty]
    private bool _isLoading;

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public MainViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        ILibraryIngestionService ingestionService)
    {
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await ReloadAsync(rescan: true, cancellationToken).ConfigureAwait(false);
    }

    public Task QueueSongAsync(SongDto song, CancellationToken cancellationToken)
    {
        return _playbackService.QueueAsync(song, cancellationToken);
    }

    public async Task ReloadAsync(bool rescan, CancellationToken cancellationToken)
    {
        try
        {
            IsLoading = true;
            if (rescan)
            {
                await _ingestionService.ScanAsync(cancellationToken).ConfigureAwait(false);
            }
            Songs = await _libraryService.GetAllSongsAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
    }
}
