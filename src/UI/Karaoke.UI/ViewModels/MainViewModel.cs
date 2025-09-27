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

    private static readonly Dictionary<char, char> TraditionalToSimplifiedMap = new()
    {
        // Common traditional to simplified mappings for better pinyin support
        ['後'] = '后', ['來'] = '来', ['劉'] = '刘', ['華'] = '华', ['張'] = '张',
        ['陳'] = '陈', ['王'] = '王', ['李'] = '李', ['趙'] = '赵', ['錢'] = '钱',
        ['孫'] = '孙', ['周'] = '周', ['吳'] = '吴', ['鄭'] = '郑', ['馮'] = '冯',
        ['陰'] = '阴', ['褚'] = '褚', ['衛'] = '卫', ['蔣'] = '蒋', ['沈'] = '沈',
        ['韓'] = '韩', ['楊'] = '杨', ['朱'] = '朱', ['秦'] = '秦', ['尤'] = '尤',
        ['許'] = '许', ['何'] = '何', ['呂'] = '吕', ['施'] = '施', 
        ['孔'] = '孔', ['曹'] = '曹', ['嚴'] = '严', ['金'] = '金',
        ['魏'] = '魏', ['陶'] = '陶', ['姜'] = '姜', ['戚'] = '戚', ['謝'] = '谢',
        ['鄒'] = '邹', ['喻'] = '喻', ['柏'] = '柏', ['水'] = '水', ['竇'] = '窦',
        ['章'] = '章', ['雲'] = '云', ['蘇'] = '苏', ['潘'] = '潘', ['葛'] = '葛',
        ['奚'] = '奚', ['範'] = '范', ['彭'] = '彭', ['郎'] = '郎', ['魯'] = '鲁',
        ['韋'] = '韦', ['昌'] = '昌', ['馬'] = '马', ['苗'] = '苗', ['鳳'] = '凤',
        ['花'] = '花', ['方'] = '方', ['俞'] = '俞', ['任'] = '任', ['袁'] = '袁',
        ['柳'] = '柳', ['酆'] = '酆', ['鮑'] = '鲍', ['史'] = '史', ['唐'] = '唐',
        ['費'] = '费', ['廉'] = '廉', ['岑'] = '岑', ['薛'] = '薛', ['雷'] = '雷',
        ['賀'] = '贺', ['倪'] = '倪', ['湯'] = '汤', ['滕'] = '滕', ['殷'] = '殷',
        ['羅'] = '罗', ['畢'] = '毕', ['郝'] = '郝', ['鄔'] = '邬', ['安'] = '安',
        ['常'] = '常', ['樂'] = '乐', ['於'] = '于', ['時'] = '时', ['傅'] = '傅',
        ['皮'] = '皮', ['卞'] = '卞', ['齊'] = '齐', ['康'] = '康', ['伍'] = '伍',
        ['余'] = '余', ['元'] = '元', ['卜'] = '卜', ['顧'] = '顾', ['孟'] = '孟',
        ['平'] = '平', ['黃'] = '黄', ['和'] = '和', ['穆'] = '穆', ['蕭'] = '萧',
        ['尹'] = '尹', ['姚'] = '姚', ['邵'] = '邵', ['湛'] = '湛', ['汪'] = '汪',
        ['祁'] = '祁', ['毛'] = '毛', ['禹'] = '禹', ['狄'] = '狄', ['米'] = '米',
        ['貝'] = '贝', ['明'] = '明', ['臧'] = '臧', ['計'] = '计', ['伏'] = '伏',
        ['成'] = '成', ['戴'] = '戴', ['談'] = '谈', ['宋'] = '宋', ['茅'] = '茅',
        ['龐'] = '庞', ['熊'] = '熊', ['紀'] = '纪', ['舒'] = '舒', ['屈'] = '屈',
        ['項'] = '项', ['祝'] = '祝', ['董'] = '董', ['梁'] = '梁', ['杜'] = '杜',
        ['阮'] = '阮', ['藍'] = '蓝', ['閔'] = '闵', ['席'] = '席', ['季'] = '季',
        ['麻'] = '麻', ['強'] = '强', ['賈'] = '贾', ['路'] = '路', ['婁'] = '娄',
        ['危'] = '危', ['江'] = '江', ['童'] = '童', ['顏'] = '颜', ['郭'] = '郭',
        ['梅'] = '梅', ['盛'] = '盛', ['林'] = '林', ['刁'] = '刁', ['鍾'] = '钟',
        ['徐'] = '徐', ['邱'] = '邱', ['駱'] = '骆', ['高'] = '高', ['夏'] = '夏',
        ['蔡'] = '蔡', ['田'] = '田', ['樊'] = '樊', ['胡'] = '胡', ['凌'] = '凌',
        ['霍'] = '霍', ['虞'] = '虞', ['萬'] = '万', ['支'] = '支', ['柯'] = '柯',
        ['昝'] = '昝', ['管'] = '管', ['盧'] = '卢', ['莫'] = '莫', ['經'] = '经',
        ['房'] = '房', ['裘'] = '裘', ['繆'] = '缪', ['干'] = '干', ['解'] = '解',
        ['應'] = '应', ['宗'] = '宗', ['丁'] = '丁', ['宣'] = '宣', ['賁'] = '贲',
        ['鄧'] = '邓', ['郁'] = '郁', ['單'] = '单', ['杭'] = '杭', ['洪'] = '洪',
        ['包'] = '包', ['諸'] = '诸', ['左'] = '左', ['石'] = '石', ['崔'] = '崔',
        ['吉'] = '吉', ['鈕'] = '钮', ['龔'] = '龚', ['程'] = '程', ['嵇'] = '嵇',
        ['邢'] = '邢', ['滑'] = '滑', ['裴'] = '裴', ['陸'] = '陆', ['榮'] = '荣',
        ['翁'] = '翁', ['荀'] = '荀', ['羊'] = '羊', ['惠'] = '惠',
        ['甄'] = '甄', ['麴'] = '麴', ['家'] = '家', ['封'] = '封', ['芮'] = '芮',
        ['羿'] = '羿', ['儲'] = '储', ['靳'] = '靳', ['汲'] = '汲', ['邴'] = '邴',
        ['糜'] = '糜', ['松'] = '松', ['井'] = '井', ['段'] = '段', ['富'] = '富',
        ['巫'] = '巫', ['烏'] = '乌', ['焦'] = '焦', ['巴'] = '巴', ['弓'] = '弓',
        ['牧'] = '牧', ['隗'] = '隗', ['山'] = '山', ['谷'] = '谷', ['車'] = '车',
        ['侯'] = '侯', ['宓'] = '宓', ['蓬'] = '蓬', ['全'] = '全', ['郗'] = '郗',
        ['班'] = '班', ['仰'] = '仰', ['秋'] = '秋', ['仲'] = '仲', ['伊'] = '伊',
        ['宮'] = '宫', ['寧'] = '宁', ['仇'] = '仇', ['欒'] = '栾', ['暴'] = '暴',
        ['甘'] = '甘', ['鈞'] = '钧', ['厲'] = '厉', ['戎'] = '戎', ['祖'] = '祖',
        ['武'] = '武', ['符'] = '符', ['景'] = '景', ['詹'] = '詹',
        ['束'] = '束', ['龍'] = '龙', ['葉'] = '叶', ['幸'] = '幸', ['司'] = '司',
        ['韶'] = '韶', ['郜'] = '郜', ['黎'] = '黎', ['薊'] = '蓟', ['薄'] = '薄',
        ['印'] = '印', ['宿'] = '宿', ['白'] = '白', ['懷'] = '怀', ['蒲'] = '蒲',
        ['邰'] = '邰', ['從'] = '从', ['鄂'] = '鄂', ['索'] = '索', ['咸'] = '咸',
        ['籍'] = '籍', ['賴'] = '赖', ['卓'] = '卓', ['藺'] = '蔺', ['屠'] = '屠',
        ['蒙'] = '蒙', ['池'] = '池', ['喬'] = '乔', ['鬱'] = '郁',
        ['胥'] = '胥', ['能'] = '能', ['蒼'] = '苍', ['雙'] = '双', ['聞'] = '闻',
        ['莘'] = '莘', ['黨'] = '党', ['翟'] = '翟', ['譚'] = '谭', ['貢'] = '贡',
        ['勞'] = '劳', ['逄'] = '逄', ['姬'] = '姬', ['申'] = '申', ['扶'] = '扶',
        ['堵'] = '堵', ['冉'] = '冉', ['宰'] = '宰', ['酈'] = '郦', ['雍'] = '雍',
        ['卻'] = '却', ['璩'] = '璩', ['桑'] = '桑', ['桂'] = '桂', ['濮'] = '濮',
        ['牛'] = '牛', ['壽'] = '寿', ['通'] = '通', ['邊'] = '边', ['扈'] = '扈',
        ['燕'] = '燕', ['冀'] = '冀', ['浦'] = '浦', ['尚'] = '尚', ['農'] = '农',
        ['溫'] = '温', ['別'] = '别', ['莊'] = '庄', ['晏'] = '晏', ['柴'] = '柴',
        ['瞿'] = '瞿', ['閻'] = '阎', ['充'] = '充', ['慕'] = '慕', ['連'] = '连',
        ['茹'] = '茹', ['習'] = '习', ['宦'] = '宦', ['艾'] = '艾', ['魚'] = '鱼',
        ['容'] = '容', ['向'] = '向', ['古'] = '古', ['易'] = '易', ['慎'] = '慎',
        ['戈'] = '戈', ['廖'] = '廖', ['庾'] = '庾', ['終'] = '终', ['暨'] = '暨',
        ['居'] = '居', ['衡'] = '衡', ['步'] = '步', ['都'] = '都', ['耿'] = '耿',
        ['滿'] = '满', ['弘'] = '弘', ['匡'] = '匡', ['國'] = '国', ['文'] = '文',
        ['寇'] = '寇', ['廣'] = '广', ['祿'] = '禄', ['闕'] = '阙', ['東'] = '东',
        ['歐'] = '欧', ['殳'] = '殳', ['沃'] = '沃', ['利'] = '利', ['蔚'] = '蔚',
        ['越'] = '越', ['夔'] = '夔', ['隆'] = '隆', ['師'] = '师', ['鞏'] = '巩',
        ['厙'] = '厙', ['聶'] = '聂', ['晁'] = '晁', ['勾'] = '勾', ['敖'] = '敖',
        ['融'] = '融', ['冷'] = '冷', ['訾'] = '訾', ['辛'] = '辛', ['闞'] = '阚',
        ['那'] = '那', ['簡'] = '简', ['饒'] = '饶', ['空'] = '空', ['曾'] = '曾',
        ['毋'] = '毋', ['沙'] = '沙', ['乜'] = '乜', ['養'] = '养', ['鞠'] = '鞠',
        ['須'] = '须', ['豐'] = '丰', ['巢'] = '巢', ['關'] = '关', ['蒯'] = '蒯',
        ['相'] = '相', ['查'] = '查', ['荊'] = '荆', ['紅'] = '红',
        ['游'] = '游', ['竺'] = '竺', ['權'] = '权', ['逯'] = '逯', ['蓋'] = '盖',
        ['益'] = '益', ['桓'] = '桓', ['公'] = '公', ['晉'] = '晋',
        ['楚'] = '楚', ['閆'] = '闫', ['法'] = '法', ['汝'] = '汝', ['鄢'] = '鄢',
        ['涂'] = '涂', ['欽'] = '钦', ['歸'] = '归',
        ['海'] = '海', ['嶽'] = '岳',
        ['帥'] = '帅', ['緱'] = '缑', ['亢'] = '亢', ['況'] = '况', ['后'] = '后',
        ['有'] = '有', ['琴'] = '琴', ['商'] = '商', ['牟'] = '牟',
        ['佘'] = '佘', ['佴'] = '佴', ['伯'] = '伯', ['賞'] = '赏',
        ['墨'] = '墨', ['哈'] = '哈', ['譙'] = '谯', ['笪'] = '笪', ['年'] = '年',
        ['愛'] = '爱', ['陽'] = '阳', ['佟'] = '佟',
        ['言'] = '言', ['福'] = '福'
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
    private readonly ObservableCollection<SongDto> _queueItems = new();

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

    private async void RemoveFromQueue()
    {
        if (SelectedQueuedSong is null)
        {
            return;
        }

        try
        {
            var songToRemove = SelectedQueuedSong;
            
            // Remove from both UI queue and playback service queue
            var removedFromQueue = await _playbackService.RemoveFromQueueAsync(songToRemove, CancellationToken.None).ConfigureAwait(false);
            
            // Also try to cancel if it's the current song (already dequeued but not yet played)
            var canceledCurrent = await _playbackService.CancelCurrentSongAsync(songToRemove, CancellationToken.None).ConfigureAwait(false);
            
            // Always remove from UI queue regardless of playback service result
            _queueItems.Remove(songToRemove);
            SelectedQueuedSong = null;
            UpdateQueuePage();
            
            if (canceledCurrent)
            {
                System.Diagnostics.Debug.WriteLine($"Canceled current song: {songToRemove.Id}");
            }
            else if (removedFromQueue)
            {
                System.Diagnostics.Debug.WriteLine($"Removed song from queue: {songToRemove.Id}");
            }
        }
        catch (Exception ex)
        {
            // Log error but still remove from UI to keep UI in sync
            System.Diagnostics.Debug.WriteLine($"Error removing song from playback queue: {ex.Message}");
            _queueItems.Remove(SelectedQueuedSong);
            SelectedQueuedSong = null;
            UpdateQueuePage();
        }
    }

    private bool CanRemoveFromQueue() => SelectedQueuedSong is not null;

    public async Task MoveQueuedSongUpAsync()
    {
        if (SelectedQueuedSong is null) return;
        
        var currentIndex = _queueItems.IndexOf(SelectedQueuedSong);
        if (currentIndex > 0)
        {
            var newPosition = currentIndex - 1;
            
            // Move in UI queue
            _queueItems.Move(currentIndex, newPosition);
            
            // Calculate and move in playback queue
            var playbackPosition = await CalculatePlaybackPositionAsync(newPosition);
            await _playbackService.MoveInQueueAsync(SelectedQueuedSong, playbackPosition, CancellationToken.None).ConfigureAwait(false);
            
            UpdateQueuePage();
        }
    }

    public async Task MoveQueuedSongToTopAsync()
    {
        if (SelectedQueuedSong is null) return;
        
        var currentIndex = _queueItems.IndexOf(SelectedQueuedSong);
        if (currentIndex <= 0) return;

        try
        {
            // Get the currently playing song to determine the correct "next" position
            var currentSong = await _playbackService.GetCurrentAsync(CancellationToken.None).ConfigureAwait(false);
            
            int uiTargetPosition = 0;
            
            if (currentSong != null)
            {
                // Find the currently playing song in our UI queue
                var currentSongIndex = -1;
                for (int i = 0; i < _queueItems.Count; i++)
                {
                    if (_queueItems[i].Id == currentSong.Id)
                    {
                        currentSongIndex = i;
                        break;
                    }
                }
                
                // Move to position after the current song (or beginning if not found)
                uiTargetPosition = currentSongIndex >= 0 ? currentSongIndex + 1 : 0;
            }
            
            // Don't move if already at target position
            if (uiTargetPosition == currentIndex) return;
            
            // Adjust UI target position if moving from later in the queue
            if (currentIndex < uiTargetPosition)
            {
                uiTargetPosition--;
            }
            
            // Move in UI queue first
            _queueItems.Move(currentIndex, uiTargetPosition);
            
            // Calculate playback queue position: it's the position relative to songs that come after current
            var playbackQueuePosition = await CalculatePlaybackPositionAsync(uiTargetPosition);
            
            // Move in playback queue
            await _playbackService.MoveInQueueAsync(SelectedQueuedSong, playbackQueuePosition, CancellationToken.None).ConfigureAwait(false);
            
            UpdateQueuePage();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error moving song to next position: {ex.Message}");
        }
    }

    public async Task MoveQueuedSongDownAsync()
    {
        if (SelectedQueuedSong is null) return;
        
        var currentIndex = _queueItems.IndexOf(SelectedQueuedSong);
        if (currentIndex >= 0 && currentIndex < _queueItems.Count - 1)
        {
            var newPosition = currentIndex + 1;
            
            // Move in UI queue
            _queueItems.Move(currentIndex, newPosition);
            
            // Calculate and move in playback queue
            var playbackPosition = await CalculatePlaybackPositionAsync(newPosition);
            await _playbackService.MoveInQueueAsync(SelectedQueuedSong, playbackPosition, CancellationToken.None).ConfigureAwait(false);
            
            UpdateQueuePage();
        }
    }

    public async Task MoveQueuedSongToBottomAsync()
    {
        if (SelectedQueuedSong is null) return;
        
        var currentIndex = _queueItems.IndexOf(SelectedQueuedSong);
        if (currentIndex >= 0 && currentIndex < _queueItems.Count - 1)
        {
            var newPosition = _queueItems.Count - 1;
            
            // Move in UI queue
            _queueItems.Move(currentIndex, newPosition);
            
            // Calculate and move in playback queue
            var playbackPosition = await CalculatePlaybackPositionAsync(newPosition);
            await _playbackService.MoveInQueueAsync(SelectedQueuedSong, playbackPosition, CancellationToken.None).ConfigureAwait(false);
            
            UpdateQueuePage();
        }
    }

    public bool CanMoveQueuedSongUp() => SelectedQueuedSong is not null && _queueItems.IndexOf(SelectedQueuedSong) > 0;

    public bool CanMoveQueuedSongToTop() => SelectedQueuedSong is not null && _queueItems.IndexOf(SelectedQueuedSong) > 0;

    public bool CanMoveQueuedSongDown() => SelectedQueuedSong is not null && _queueItems.IndexOf(SelectedQueuedSong) >= 0 && _queueItems.IndexOf(SelectedQueuedSong) < _queueItems.Count - 1;

    public bool CanMoveQueuedSongToBottom() => SelectedQueuedSong is not null && _queueItems.IndexOf(SelectedQueuedSong) >= 0 && _queueItems.IndexOf(SelectedQueuedSong) < _queueItems.Count - 1;

    private async Task<int> CalculatePlaybackPositionAsync(int uiPosition)
    {
        try
        {
            // Get the currently playing song
            var currentSong = await _playbackService.GetCurrentAsync(CancellationToken.None).ConfigureAwait(false);
            
            if (currentSong == null)
            {
                // No current song, playback queue matches UI queue
                return uiPosition;
            }
            
            // Find the currently playing song in UI queue
            var currentSongIndex = -1;
            for (int i = 0; i < _queueItems.Count; i++)
            {
                if (_queueItems[i].Id == currentSong.Id)
                {
                    currentSongIndex = i;
                    break;
                }
            }
            
            if (currentSongIndex < 0)
            {
                // Current song not found in UI queue, assume it's at beginning
                return Math.Max(0, uiPosition);
            }
            
            // Playback queue starts after the current song
            // So UI position N maps to playback position (N - currentSongIndex - 1)
            var playbackPosition = uiPosition - currentSongIndex - 1;
            return Math.Max(0, playbackPosition);
        }
        catch
        {
            // Fallback to UI position if anything goes wrong
            return uiPosition;
        }
    }


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

        // First try to convert traditional Chinese to simplified
        var targetChar = ch;
        if (TraditionalToSimplifiedMap.TryGetValue(ch, out var simplified))
        {
            targetChar = simplified;
        }

        try
        {
            var bytes = Gb2312Encoding.GetBytes(new[] { targetChar });
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








