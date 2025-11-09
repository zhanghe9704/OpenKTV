using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karaoke.Library.Configuration;

namespace Karaoke.UI.ViewModels.Settings;

public sealed class RescanRequestedEventArgs : EventArgs
{
    public RescanRequestedEventArgs(bool rescanAll, IReadOnlyList<string> rootsToRescan, IReadOnlyList<string> rootsAddNewOnly, IReadOnlyList<string> rootsNormalizeOnly)
    {
        RescanAll = rescanAll;
        RootsToRescan = rootsToRescan ?? throw new ArgumentNullException(nameof(rootsToRescan));
        RootsAddNewOnly = rootsAddNewOnly ?? throw new ArgumentNullException(nameof(rootsAddNewOnly));
        RootsNormalizeOnly = rootsNormalizeOnly ?? throw new ArgumentNullException(nameof(rootsNormalizeOnly));
    }

    public bool RescanAll { get; }
    public IReadOnlyList<string> RootsToRescan { get; }
    public IReadOnlyList<string> RootsAddNewOnly { get; }
    public IReadOnlyList<string> RootsNormalizeOnly { get; }
}

public partial class LibrarySettingsViewModel : ObservableObject
{
    private readonly ILibraryConfigurationManager _configurationManager;

    public LibrarySettingsViewModel(ILibraryConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        Roots = new ObservableCollection<LibraryRootItemViewModel>();
        LoadCommand = new AsyncRelayCommand(LoadAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        AddRootCommand = new RelayCommand(AddRoot);
        RemoveSelectedRootCommand = new RelayCommand(RemoveSelectedRoot, () => SelectedRoot is not null);
        RescanAfterSave = false;
    }

    public ObservableCollection<LibraryRootItemViewModel> Roots { get; }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IRelayCommand AddRootCommand { get; }

    public IRelayCommand RemoveSelectedRootCommand { get; }

    [ObservableProperty]
    private LibraryRootItemViewModel? _selectedRoot;

    partial void OnSelectedRootChanged(LibraryRootItemViewModel? value)
    {
        RemoveSelectedRootCommand.NotifyCanExecuteChanged();
    }

    [ObservableProperty]
    private bool _rescanAfterSave;

    [ObservableProperty]
    private string? _globalKeywordFormat;

    [ObservableProperty]
    private bool _showArtist;

    [ObservableProperty]
    private bool _showLanguage;

    [ObservableProperty]
    private bool _showGenre;

    [ObservableProperty]
    private bool _showComment;

    [ObservableProperty]
    private bool _showChannel;

    [ObservableProperty]
    private bool _showPriority;

    public event EventHandler? SettingsSaved;

    public event EventHandler<RescanRequestedEventArgs>? RescanRequested;

    public IEnumerable<string> GetRootsToRescan()
    {
        return Roots.Where(r => r.ShouldRescan).Select(r => r.Name);
    }

    public async Task LoadAsync()
    {
        var libraryOptions = await _configurationManager.GetLibraryOptionsAsync(CancellationToken.None).ConfigureAwait(false);
        GlobalKeywordFormat = libraryOptions.KeywordFormat;

        ShowArtist = libraryOptions.DisplayOptions.ShowArtist;
        ShowLanguage = libraryOptions.DisplayOptions.ShowLanguage;
        ShowGenre = libraryOptions.DisplayOptions.ShowGenre;
        ShowComment = libraryOptions.DisplayOptions.ShowComment;
        ShowChannel = libraryOptions.DisplayOptions.ShowChannel;
        ShowPriority = libraryOptions.DisplayOptions.ShowPriority;

        Roots.Clear();
        foreach (var root in libraryOptions.Roots)
        {
            Roots.Add(new LibraryRootItemViewModel(root.Name, root.Path, root.DefaultPriority, root.DefaultChannel, root.DriveOverride, root.KeywordFormat, root.Instrumental, shouldRescan: false, volumeNormalization: root.VolumeNormalization, addNewSongsOnly: root.AddNewSongsOnly));
        }
        RescanAfterSave = false;
    }

    public async Task SaveAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"SaveAsync: GlobalKeywordFormat = '{GlobalKeywordFormat}'");
            System.Diagnostics.Debug.WriteLine($"SaveAsync: Roots count = {Roots.Count}");
            
            foreach (var root in Roots)
            {
                System.Diagnostics.Debug.WriteLine($"  Root: Name='{root.Name}', Path='{root.Path}', Priority='{root.DefaultPriority}', Channel='{root.DefaultChannel}', DriveOverride='{root.DriveOverride}', KeywordFormat='{root.KeywordFormat}', Instrumental='{root.Instrumental}', InstrumentalInt={root.GetInstrumental()}, ShouldRescan={root.ShouldRescan}");
            }
            
            // Load the base options to preserve settings like SupportedExtensions and DatabasePath
            var baseOptions = await _configurationManager.GetLibraryOptionsAsync(CancellationToken.None).ConfigureAwait(false);

            // Create a fresh options object with updated values from UI
            var updatedOptions = new LibraryOptions
            {
                DefaultPriority = baseOptions.DefaultPriority,
                DefaultChannel = baseOptions.DefaultChannel,
                DatabasePath = baseOptions.DatabasePath,
                SupportedExtensions = baseOptions.SupportedExtensions.Distinct().ToList(), // Deduplicate extensions
                KeywordFormat = string.IsNullOrWhiteSpace(GlobalKeywordFormat) ? null : GlobalKeywordFormat,
                DisplayOptions = new SongDisplayOptions
                {
                    ShowArtist = ShowArtist,
                    ShowLanguage = ShowLanguage,
                    ShowGenre = ShowGenre,
                    ShowComment = ShowComment,
                    ShowChannel = ShowChannel,
                    ShowPriority = ShowPriority
                },
                Roots = Roots
                    .Select(root =>
                    {
                        System.Diagnostics.Debug.WriteLine($"SaveAsync: Root '{root.Name}' VolumeNormalization = {root.VolumeNormalization}, AddNewSongsOnly = {root.AddNewSongsOnly}");
                        return new LibraryRootOptions
                        {
                            Name = root.Name,
                            Path = root.Path,
                            DefaultPriority = root.GetPriority(),
                            DefaultChannel = root.DefaultChannel,
                            DriveOverride = string.IsNullOrWhiteSpace(root.DriveOverride) ? null : root.DriveOverride,
                            KeywordFormat = string.IsNullOrWhiteSpace(root.KeywordFormat) ? null : root.KeywordFormat,
                            Instrumental = root.GetInstrumental(),
                            VolumeNormalization = root.VolumeNormalization,
                            AddNewSongsOnly = root.AddNewSongsOnly,
                        };
                    })
                    .ToList()
            };

            await _configurationManager.SaveLibraryOptionsAsync(updatedOptions, CancellationToken.None).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine("SaveAsync: Configuration saved successfully");

            // Give the configuration system time to reload the settings file
            // IOptionsMonitor uses file watchers which need time to detect changes
            await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);

            // Determine what needs rescanning
            // Scan folders if: ShouldRescan is checked, AddNewSongsOnly is checked, or VolumeNormalization is checked
            var rootsToRescan = Roots.Where(r => r.ShouldRescan || r.AddNewSongsOnly || r.VolumeNormalization).Select(r => r.Name).ToList();
            var rootsAddNewOnly = Roots.Where(r => r.AddNewSongsOnly && !r.ShouldRescan).Select(r => r.Name).ToList();
            var rootsNormalizeOnly = Roots.Where(r => r.VolumeNormalization && !r.ShouldRescan && !r.AddNewSongsOnly).Select(r => r.Name).ToList();
            var shouldRescanAll = RescanAfterSave;

            SettingsSaved?.Invoke(this, EventArgs.Empty);

            if (shouldRescanAll || rootsToRescan.Count > 0)
            {
                RescanRequested?.Invoke(this, new RescanRequestedEventArgs(shouldRescanAll, rootsToRescan, rootsAddNewOnly, rootsNormalizeOnly));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Exception in SaveAsync: {ex}");
            throw; // Re-throw to let the UI handle it
        }
    }

    private void AddRoot()
    {
        Roots.Add(new LibraryRootItemViewModel("New_folder", "", defaultPriority: 2, defaultChannel: "Stereo", driveOverride: null, keywordFormat: null, instrumental: 0, shouldRescan: true, volumeNormalization: false, addNewSongsOnly: false));
        SelectedRoot = Roots.Last();
    }

    private void RemoveSelectedRoot()
    {
        if (SelectedRoot is null)
        {
            return;
        }

        Roots.Remove(SelectedRoot);
        SelectedRoot = null;
    }
}
