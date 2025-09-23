using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karaoke.Library.Configuration;

namespace Karaoke.UI.ViewModels.Settings;

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
        RescanAfterSave = true;
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

    public event EventHandler? SettingsSaved;

    public async Task LoadAsync()
    {
        var roots = await _configurationManager.GetRootsAsync(CancellationToken.None).ConfigureAwait(false);

        Roots.Clear();
        foreach (var root in roots)
        {
            Roots.Add(new LibraryRootItemViewModel(root.Name, root.Path, root.DefaultPriority, root.DefaultChannel, root.DriveOverride));
        }
        RescanAfterSave = true;
    }

    public async Task SaveAsync()
    {
        var updatedRoots = Roots
            .Select(root => new LibraryRootOptions
            {
                Name = root.Name,
                Path = root.Path,
                DefaultPriority = root.GetPriority(),
                DefaultChannel = root.DefaultChannel,
                DriveOverride = string.IsNullOrWhiteSpace(root.DriveOverride) ? null : root.DriveOverride,
            })
            .ToList();

        await _configurationManager.SaveRootsAsync(updatedRoots, CancellationToken.None).ConfigureAwait(false);
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void AddRoot()
    {
        Roots.Add(new LibraryRootItemViewModel("NewRoot", "", defaultPriority: 2, defaultChannel: "Stereo", driveOverride: null));
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
