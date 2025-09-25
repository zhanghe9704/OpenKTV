using System.Threading;
using Karaoke.Common.Models;
using Karaoke.UI.ViewModels;
using Karaoke.UI.ViewModels.Settings;
using Karaoke.UI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Karaoke.UI;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly LibrarySettingsViewModel _settingsViewModel;

    public MainWindow(MainViewModel viewModel, LibrarySettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsViewModel = settingsViewModel;

        if (Content is FrameworkElement element)
        {
            element.DataContext = _viewModel;
            element.Loaded += OnLoaded;
        }
    }

    public MainViewModel ViewModel => _viewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Loaded -= OnLoaded;
        }

        await _viewModel.InitializeAsync(CancellationToken.None).ConfigureAwait(true);
    }

    private async void OnSongItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not SongDto clickedSong)
        {
            return;
        }

        ViewModel.SelectedSong = clickedSong;

        if (ViewModel.AddToQueueCommand.CanExecute(null))
        {
            await ViewModel.AddToQueueCommand.ExecuteAsync(null).ConfigureAwait(true);
        }
    }

    private async void OnRescanClicked(object sender, RoutedEventArgs e)
    {
        await _viewModel.ReloadAsync(rescan: true, CancellationToken.None).ConfigureAwait(false);
    }

    private async void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        await _settingsViewModel.LoadAsync().ConfigureAwait(true);

        var dialog = new SettingsDialog(_settingsViewModel)
        {
            XamlRoot = (Content as FrameworkElement)?.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.ReloadAsync(_settingsViewModel.RescanAfterSave, CancellationToken.None).ConfigureAwait(false);
        }
    }
}