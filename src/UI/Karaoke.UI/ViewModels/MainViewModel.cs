using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karaoke.Common.Models;
using Karaoke.Library.Ingestion;
using Karaoke.Library.Services;
using Karaoke.Player.Playback;
using Microsoft.UI.Xaml;

namespace Karaoke.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryIngestionService _ingestionService;
    private List<SongDto> _allSongs = new();

    [ObservableProperty]
    private IReadOnlyList<SongDto> _songs = Array.Empty<SongDto>();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _selectedArtist;

    [ObservableProperty]
    private SongDto? _selectedSong;

    [ObservableProperty]
    private SongDto? _selectedQueuedSong;

    public ObservableCollection<string> Artists { get; } = new();

    public ObservableCollection<SongDto> FilteredSongs { get; } = new();

    public ObservableCollection<SongDto> Queue { get; } = new();

    public MainViewModel(
        ILibraryService libraryService,
        IPlaybackService playbackService,
        ILibraryIngestionService ingestionService)
    {
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));
        _playbackService = playbackService ?? throw new ArgumentNullException(nameof(playbackService));
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));

        AddToQueueCommand = new AsyncRelayCommand(AddToQueueAsync, CanAddToQueue);
        RemoveFromQueueCommand = new RelayCommand(RemoveFromQueue, CanRemoveFromQueue);
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public IAsyncRelayCommand AddToQueueCommand { get; }

    public IRelayCommand RemoveFromQueueCommand { get; }


    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await ReloadAsync(rescan: false, cancellationToken).ConfigureAwait(false);

        if (Songs.Count == 0)
        {
            await ReloadAsync(rescan: true, cancellationToken).ConfigureAwait(false);
        }
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

            var songs = await _libraryService.GetAllSongsAsync(cancellationToken).ConfigureAwait(false);
            _allSongs = songs.ToList();
            Songs = songs;

            UpdateArtists();
            UpdateFilteredSongs();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddToQueueAsync()
    {
        if (SelectedSong is null)
        {
            return;
        }

        Queue.Add(SelectedSong);
        await _playbackService.QueueAsync(SelectedSong, CancellationToken.None).ConfigureAwait(false);
    }

    private bool CanAddToQueue() => SelectedSong is not null;

    private void RemoveFromQueue()
    {
        if (SelectedQueuedSong is null)
        {
            return;
        }

        Queue.Remove(SelectedQueuedSong);
        SelectedQueuedSong = null;
    }

    private bool CanRemoveFromQueue() => SelectedQueuedSong is not null;

    partial void OnSelectedSongChanged(SongDto? value)
    {
        AddToQueueCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedQueuedSongChanged(SongDto? value)
    {
        RemoveFromQueueCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedArtistChanged(string? value)
    {
        UpdateFilteredSongs();
    }

    private void UpdateArtists()
    {
        var distinctArtists = _allSongs
            .Select(song => song.Artist)
            .Where(artist => !string.IsNullOrWhiteSpace(artist))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(artist => artist, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Artists.Clear();
        foreach (var artist in distinctArtists)
        {
            Artists.Add(artist);
        }

        if (Artists.Count > 0 && (SelectedArtist is null || !Artists.Contains(SelectedArtist)))
        {
            SelectedArtist = Artists.First();
        }
        else if (Artists.Count == 0)
        {
            SelectedArtist = null;
        }
    }

    private void UpdateFilteredSongs()
    {
        IEnumerable<SongDto> songs = _allSongs;

        if (!string.IsNullOrWhiteSpace(SelectedArtist))
        {
            songs = songs.Where(song => string.Equals(song.Artist, SelectedArtist, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = songs
            .OrderBy(song => song.Priority)
            .ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        FilteredSongs.Clear();
        foreach (var song in ordered)
        {
            FilteredSongs.Add(song);
        }

        if (SelectedSong is null || !FilteredSongs.Contains(SelectedSong))
        {
            SelectedSong = FilteredSongs.FirstOrDefault();
        }
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
    }
}



