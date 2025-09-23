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
    }

    public ObservableCollection<LibraryRootItemViewModel> Roots { get; }

    public IAsyncRelayCommand LoadCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public event EventHandler? SettingsSaved;

    public async Task LoadAsync()
    {
        var roots = await _configurationManager.GetRootsAsync(CancellationToken.None).ConfigureAwait(false);

        Roots.Clear();
        foreach (var root in roots)
        {
            Roots.Add(new LibraryRootItemViewModel(root.Name, root.Path, root.DefaultPriority, root.DefaultChannel, root.DriveOverride));
        }
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
}
