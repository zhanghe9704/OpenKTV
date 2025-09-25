using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
    private const int PageSize = 20;

    private static readonly Encoding Gb2312Encoding;
    private static readonly int[] PinyinCodeThresholds =
    {
        0xB0A1, 0xB0C5, 0xB2C1, 0xB4EE, 0xB6EA,
        0xB7A2, 0xB8C1, 0xB9FE, 0xBBF7, 0xBFA6,
        0xC0AC, 0xC2E8, 0xC4C3, 0xC5B6, 0xC5BE,
        0xC6DA, 0xC8BB, 0xC8F6, 0xCBFA, 0xCDDA,
        0xCEF4, 0xD1B9, 0xD4D1, 0xD7FA
    };

    private static readonly char[] PinyinInitials =
    {
        'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
        'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q',
        'R', 'S', 'T', 'W', 'X', 'Y', 'Z'
    };

    static MainViewModel()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Gb2312Encoding = Encoding.GetEncoding("GB2312");
    }

    private readonly ILibraryService _libraryService;
    private readonly IPlaybackService _playbackService;
    private readonly ILibraryIngestionService _ingestionService;

    private List<SongDto> _allSongs = new();
    private List<string> _allArtists = new();
    private List<string> _filteredArtistsSource = new();
    private List<SongDto> _filteredSongsSource = new();
    private readonly List<SongDto> _queueItems = new();

    private readonly Dictionary<string, string> _artistInitials = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (string TitleInitials, string ArtistInitials)> _songInitialsById = new();

    private int _artistsPageIndex;
    private int _songsPageIndex;
    private int _queuePageIndex;

    private readonly RelayCommand _artistsPreviousPageCommand;
    private readonly RelayCommand _artistsNextPageCommand;
    private readonly RelayCommand _songsPreviousPageCommand;
    private readonly RelayCommand _songsNextPageCommand;
    private readonly RelayCommand _queuePreviousPageCommand;
    private readonly RelayCommand _queueNextPageCommand;

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

    [ObservableProperty]
    private string? _artistSearchText;

    [ObservableProperty]
    private string? _songSearchText;

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
        SearchArtistsCommand = new RelayCommand(() => ApplyArtistSearch(resetPage: true));
        SearchSongsCommand = new RelayCommand(() => UpdateFilteredSongs(resetPage: true));

        _artistsPreviousPageCommand = new RelayCommand(() => MoveArtistsPage(-1), () => _artistsPageIndex > 0);
        _artistsNextPageCommand = new RelayCommand(() => MoveArtistsPage(1), CanMoveArtistsForward);
        _songsPreviousPageCommand = new RelayCommand(() => MoveSongsPage(-1), () => _songsPageIndex > 0);
        _songsNextPageCommand = new RelayCommand(() => MoveSongsPage(1), CanMoveSongsForward);
        _queuePreviousPageCommand = new RelayCommand(() => MoveQueuePage(-1), () => _queuePageIndex > 0);
        _queueNextPageCommand = new RelayCommand(() => MoveQueuePage(1), CanMoveQueueForward);

        UpdateQueuePage();
    }

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public IAsyncRelayCommand AddToQueueCommand { get; }

    public IRelayCommand RemoveFromQueueCommand { get; }

    public IRelayCommand ArtistsPreviousPageCommand => _artistsPreviousPageCommand;

    public IRelayCommand ArtistsNextPageCommand => _artistsNextPageCommand;

    public IRelayCommand SongsPreviousPageCommand => _songsPreviousPageCommand;

    public IRelayCommand SongsNextPageCommand => _songsNextPageCommand;

    public IRelayCommand QueuePreviousPageCommand => _queuePreviousPageCommand;

    public IRelayCommand QueueNextPageCommand => _queueNextPageCommand;

    public IRelayCommand SearchArtistsCommand { get; }

    public IRelayCommand SearchSongsCommand { get; }

    public double ArtistsPageNumber
    {
        get => _artistsPageIndex + 1;
        set => SetArtistsPageFromInput(value);
    }

    public double SongsPageNumber
    {
        get => _songsPageIndex + 1;
        set => SetSongsPageFromInput(value);
    }

    public double QueuePageNumber
    {
        get => _queuePageIndex + 1;
        set => SetQueuePageFromInput(value);
    }

    public int ArtistsTotalPages => CalculateTotalPages(_filteredArtistsSource.Count);

    public int SongsTotalPages => CalculateTotalPages(_filteredSongsSource.Count);

    public int QueueTotalPages => CalculateTotalPages(_queueItems.Count);

    public string ArtistsPageSummary => FormatPageSummary(_artistsPageIndex, _filteredArtistsSource.Count, Artists.Count);

    public string SongsPageSummary => FormatPageSummary(_songsPageIndex, _filteredSongsSource.Count, FilteredSongs.Count);

    public string QueuePageSummary => FormatPageSummary(_queuePageIndex, _queueItems.Count, Queue.Count);

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

            RebuildSongInitials();
            UpdateArtists();
            UpdateFilteredSongs(resetPage: true);
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

        _queueItems.Add(SelectedSong);
        await _playbackService.QueueAsync(SelectedSong, CancellationToken.None).ConfigureAwait(false);
        UpdateQueuePage();
    }

    private bool CanAddToQueue() => SelectedSong is not null;

    private void RemoveFromQueue()
    {
        if (SelectedQueuedSong is null)
        {
            return;
        }

        _queueItems.Remove(SelectedQueuedSong);
        SelectedQueuedSong = null;
        UpdateQueuePage();
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
        UpdateFilteredSongs(resetPage: true);
    }

    partial void OnArtistSearchTextChanged(string? value)
    {
        ApplyArtistSearch(resetPage: true);
    }

    partial void OnSongSearchTextChanged(string? value)
    {
        UpdateFilteredSongs(resetPage: true);
    }

    private void UpdateArtists()
    {
        _allArtists = _allSongs
            .Select(song => song.Artist)
            .Where(artist => !string.IsNullOrWhiteSpace(artist))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(artist => artist, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _artistInitials.Clear();
        foreach (var artist in _allArtists)
        {
            _artistInitials[artist] = BuildInitials(artist);
        }

        ApplyArtistSearch(resetPage: true);
    }

    private void ApplyArtistSearch(bool resetPage)
    {
        IEnumerable<string> source = _allArtists;
        var term = ArtistSearchText?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var upper = term.ToUpperInvariant();
            var initialsQuery = upper.Replace(" ", string.Empty, StringComparison.Ordinal);
            source = source.Where(artist =>
                artist.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                (_artistInitials.TryGetValue(artist, out var initials) &&
                 initials.StartsWith(initialsQuery, StringComparison.OrdinalIgnoreCase)));
        }

        _filteredArtistsSource = source.ToList();

        if (resetPage)
        {
            _artistsPageIndex = 0;
        }

        EnsurePageIndex(ref _artistsPageIndex, _filteredArtistsSource.Count);
        UpdateArtistsPage();
    }

    private void UpdateArtistsPage()
    {
        Artists.Clear();

        foreach (var artist in GetPageItems(_filteredArtistsSource, _artistsPageIndex))
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

        RaiseArtistsPagingNotifications();
    }

    private void UpdateFilteredSongs(bool resetPage = false)
    {
        IEnumerable<SongDto> songs = _allSongs;

        var term = SongSearchText?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var upper = term.ToUpperInvariant();
            var initialsQuery = upper.Replace(" ", string.Empty, StringComparison.Ordinal);

            songs = songs.Where(song =>
            {
                if (song.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                    song.Artist.Contains(term, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }

                if (!_songInitialsById.TryGetValue(song.Id, out var initials))
                {
                    return false;
                }

                return initials.TitleInitials.StartsWith(initialsQuery, StringComparison.OrdinalIgnoreCase) ||
                       initials.ArtistInitials.StartsWith(initialsQuery, StringComparison.OrdinalIgnoreCase);
            });
        }
        else if (!string.IsNullOrWhiteSpace(SelectedArtist))
        {
            songs = songs.Where(song => string.Equals(song.Artist, SelectedArtist, StringComparison.OrdinalIgnoreCase));
        }

        _filteredSongsSource = songs
            .OrderBy(song => song.Priority)
            .ThenBy(song => song.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resetPage)
        {
            _songsPageIndex = 0;
        }

        EnsurePageIndex(ref _songsPageIndex, _filteredSongsSource.Count);

        UpdateSongsPage();
    }

    private void UpdateSongsPage()
    {
        FilteredSongs.Clear();

        foreach (var song in GetPageItems(_filteredSongsSource, _songsPageIndex))
        {
            FilteredSongs.Add(song);
        }

        if (FilteredSongs.Count > 0 && (SelectedSong is null || !FilteredSongs.Contains(SelectedSong)))
        {
            SelectedSong = FilteredSongs.First();
        }
        else if (FilteredSongs.Count == 0)
        {
            SelectedSong = null;
        }

        RaiseSongsPagingNotifications();
    }

    private void RebuildSongInitials()
    {
        _songInitialsById.Clear();

        foreach (var song in _allSongs)
        {
            _songInitialsById[song.Id] = (BuildInitials(song.Title), BuildInitials(song.Artist));
        }
    }

    private void UpdateQueuePage()
    {
        EnsurePageIndex(ref _queuePageIndex, _queueItems.Count);

        Queue.Clear();
        foreach (var song in GetPageItems(_queueItems, _queuePageIndex))
        {
            Queue.Add(song);
        }

        if (Queue.Count > 0 && (SelectedQueuedSong is null || !Queue.Contains(SelectedQueuedSong)))
        {
            SelectedQueuedSong = Queue.First();
        }
        else if (Queue.Count == 0)
        {
            SelectedQueuedSong = null;
        }

        RaiseQueuePagingNotifications();
    }

    private void MoveArtistsPage(int delta)
    {
        var newIndex = Math.Clamp(_artistsPageIndex + delta, 0, Math.Max(0, ArtistsTotalPages - 1));
        if (newIndex == _artistsPageIndex)
        {
            return;
        }

        _artistsPageIndex = newIndex;
        UpdateArtistsPage();
    }

    private void MoveSongsPage(int delta)
    {
        var newIndex = Math.Clamp(_songsPageIndex + delta, 0, Math.Max(0, SongsTotalPages - 1));
        if (newIndex == _songsPageIndex)
        {
            return;
        }

        _songsPageIndex = newIndex;
        UpdateSongsPage();
    }

    private void MoveQueuePage(int delta)
    {
        var newIndex = Math.Clamp(_queuePageIndex + delta, 0, Math.Max(0, QueueTotalPages - 1));
        if (newIndex == _queuePageIndex)
        {
            return;
        }

        _queuePageIndex = newIndex;
        UpdateQueuePage();
    }

    private void SetArtistsPageFromInput(double value)
    {
        var newIndex = ConvertToPageIndex(value, ArtistsTotalPages);
        if (newIndex == _artistsPageIndex)
        {
            OnPropertyChanged(nameof(ArtistsPageNumber));
            return;
        }

        _artistsPageIndex = newIndex;
        UpdateArtistsPage();
    }

    private void SetSongsPageFromInput(double value)
    {
        var newIndex = ConvertToPageIndex(value, SongsTotalPages);
        if (newIndex == _songsPageIndex)
        {
            OnPropertyChanged(nameof(SongsPageNumber));
            return;
        }

        _songsPageIndex = newIndex;
        UpdateSongsPage();
    }

    private void SetQueuePageFromInput(double value)
    {
        var newIndex = ConvertToPageIndex(value, QueueTotalPages);
        if (newIndex == _queuePageIndex)
        {
            OnPropertyChanged(nameof(QueuePageNumber));
            return;
        }

        _queuePageIndex = newIndex;
        UpdateQueuePage();
    }

    private static int ConvertToPageIndex(double value, int totalPages)
    {
        if (totalPages <= 0)
        {
            return 0;
        }

        var clamped = Math.Clamp((int)Math.Round(value), 1, totalPages);
        return clamped - 1;
    }

    private static IReadOnlyList<T> GetPageItems<T>(IReadOnlyList<T> source, int pageIndex)
    {
        if (source.Count == 0)
        {
            return Array.Empty<T>();
        }

        var start = pageIndex * PageSize;
        var end = Math.Min(source.Count, start + PageSize);
        if (start >= end)
        {
            return Array.Empty<T>();
        }

        var buffer = new T[end - start];
        for (var i = start; i < end; i++)
        {
            buffer[i - start] = source[i];
        }

        return buffer;
    }

    private static void EnsurePageIndex(ref int pageIndex, int totalCount)
    {
        if (totalCount == 0)
        {
            pageIndex = 0;
            return;
        }

        var maxIndex = (totalCount - 1) / PageSize;
        pageIndex = Math.Clamp(pageIndex, 0, maxIndex);
    }

    private static int CalculateTotalPages(int totalCount)
    {
        return totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
    }

    private static string FormatPageSummary(int pageIndex, int totalCount, int itemsOnPage)
    {
        if (totalCount == 0)
        {
            return "0/0";
        }

        var start = pageIndex * PageSize;
        var endExclusive = start + Math.Max(itemsOnPage, 0);
        if (itemsOnPage == 0)
        {
            endExclusive = Math.Min(totalCount, start + PageSize);
        }

        return $"{start + 1}-{Math.Min(totalCount, endExclusive)}/{totalCount}";
    }

    private bool CanMoveArtistsForward()
    {
        return (_artistsPageIndex + 1) * PageSize < _filteredArtistsSource.Count;
    }

    private bool CanMoveSongsForward()
    {
        return (_songsPageIndex + 1) * PageSize < _filteredSongsSource.Count;
    }

    private bool CanMoveQueueForward()
    {
        return (_queuePageIndex + 1) * PageSize < _queueItems.Count;
    }

    private void RaiseArtistsPagingNotifications()
    {
        OnPropertyChanged(nameof(ArtistsPageSummary));
        OnPropertyChanged(nameof(ArtistsPageNumber));
        OnPropertyChanged(nameof(ArtistsTotalPages));
        _artistsPreviousPageCommand.NotifyCanExecuteChanged();
        _artistsNextPageCommand.NotifyCanExecuteChanged();
    }

    private void RaiseSongsPagingNotifications()
    {
        OnPropertyChanged(nameof(SongsPageSummary));
        OnPropertyChanged(nameof(SongsPageNumber));
        OnPropertyChanged(nameof(SongsTotalPages));
        _songsPreviousPageCommand.NotifyCanExecuteChanged();
        _songsNextPageCommand.NotifyCanExecuteChanged();
    }

    private void RaiseQueuePagingNotifications()
    {
        OnPropertyChanged(nameof(QueuePageSummary));
        OnPropertyChanged(nameof(QueuePageNumber));
        OnPropertyChanged(nameof(QueueTotalPages));
        _queuePreviousPageCommand.NotifyCanExecuteChanged();
        _queueNextPageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
    }

    private static string BuildInitials(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var ch in text)
        {
            if (IsAsciiLetter(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
            else if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
            else
            {
                var initial = GetChineseInitial(ch);
                if (initial != '\0')
                {
                    builder.Append(initial);
                }
            }
        }

        return builder.ToString();
    }

    private static bool IsAsciiLetter(char ch)
    {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private static char GetChineseInitial(char ch)
    {
        if (IsAsciiLetter(ch))
        {
            return char.ToUpperInvariant(ch);
        }

        try
        {
            var bytes = Gb2312Encoding.GetBytes(new[] { ch });
            if (bytes.Length != 2)
            {
                return '\0';
            }

            var code = (bytes[0] << 8) + bytes[1];
            for (var i = 0; i < PinyinInitials.Length; i++)
            {
                var lower = PinyinCodeThresholds[i];
                var upper = i == PinyinInitials.Length - 1 ? 0xD7FF : PinyinCodeThresholds[i + 1];
                if (code >= lower && code < upper)
                {
                    return PinyinInitials[i];
                }
            }
        }
        catch (EncoderFallbackException)
        {
        }

        return '\0';
    }
}








