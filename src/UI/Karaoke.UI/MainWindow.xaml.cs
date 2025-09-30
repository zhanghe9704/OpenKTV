using System;
using System.Threading;
using System.Threading.Tasks;
using Karaoke.Common.Models;
using Karaoke.Player.Playback;
using Karaoke.UI.ViewModels;
using Karaoke.UI.ViewModels.Settings;
using Karaoke.UI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Karaoke.UI;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly LibrarySettingsViewModel _settingsViewModel;
    private readonly IPlaybackService _playbackService;
    private readonly DispatcherTimer _currentSongRefreshTimer;

    public MainWindow(MainViewModel viewModel, LibrarySettingsViewModel settingsViewModel, IPlaybackService playbackService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _settingsViewModel = settingsViewModel;
        _playbackService = playbackService;

        _currentSongRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10) // Reduced frequency since we have event-driven updates
        };
        _currentSongRefreshTimer.Tick += OnCurrentSongRefreshTimer;

        if (Content is FrameworkElement element)
        {
            element.DataContext = _viewModel;
            element.Loaded += OnLoaded;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public MainViewModel ViewModel => _viewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            element.Loaded -= OnLoaded;
        }

        await _viewModel.InitializeAsync(CancellationToken.None).ConfigureAwait(true);
        _currentSongRefreshTimer.Start();
    }

    private async void OnCurrentSongRefreshTimer(object sender, object e)
    {
        await _viewModel.RefreshCurrentPlayingSongAsync().ConfigureAwait(true);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.CurrentPlayingQueueIndex))
        {
            RefreshQueueVisualStates();
        }
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

        // Subscribe to the rescan event
        RescanRequestedEventArgs? rescanArgs = null;
        _settingsViewModel.RescanRequested += (_, args) =>
        {
            rescanArgs = args;
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && rescanArgs != null)
        {
            if (rescanArgs.RescanAll)
            {
                // Rescan all roots
                await _viewModel.ReloadAsync(rescan: true, CancellationToken.None).ConfigureAwait(false);
            }
            else if (rescanArgs.RootsToRescan.Count > 0)
            {
                // Rescan only specific roots
                await _viewModel.ReloadSpecificRootsAsync(rescanArgs.RootsToRescan, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private async void OnPlayClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.PlayAsync(CancellationToken.None).ConfigureAwait(false);
            _viewModel.OnPlaybackStarted();
            await _viewModel.RefreshCurrentPlayingSongAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Playback error: {ex.Message}");
        }
    }

    private async void OnPauseClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.PauseAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Pause error: {ex.Message}");
        }
    }

    private async void OnStopClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Stop error: {ex.Message}");
        }
    }

    private async void OnNextClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.MoveNextAsync(CancellationToken.None).ConfigureAwait(false);
            _viewModel.OnPlaybackNextSong();
            await _viewModel.RefreshCurrentPlayingSongAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Next error: {ex.Message}");
        }
    }

    private async void OnRepeatClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.RestartCurrentSongAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Repeat error: {ex.Message}");
        }
    }

    private async void OnFullScreenClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.ToggleFullScreenAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Full Screen error: {ex.Message}");
        }
    }

    private void OnMoveUpClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanMoveQueuedSongUp())
        {
            _ = _viewModel.MoveQueuedSongUpAsync();
        }
    }

    private void OnMoveTopClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanMoveQueuedSongToTop())
        {
            _ = _viewModel.MoveQueuedSongToTopAsync();
        }
    }

    private void OnMoveDownClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanMoveQueuedSongDown())
        {
            _ = _viewModel.MoveQueuedSongDownAsync();
        }
    }

    private void OnMoveBottomClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel.CanMoveQueuedSongToBottom())
        {
            _ = _viewModel.MoveQueuedSongToBottomAsync();
        }
    }

    private void RefreshQueueVisualStates()
    {
        RefreshQueueVisualStatesWithRetry(0);
    }
    
    private void RefreshQueueVisualStatesWithRetry(int retryCount)
    {
        const int maxRetries = 3;
        
        // Hide all indicators first
        for (int i = 0; i < _viewModel.Queue.Count; i++)
        {
            var item = QueueListView.ContainerFromIndex(i) as ListViewItem;
            if (item?.ContentTemplateRoot is FrameworkElement root)
            {
                var indicator = FindElementByName(root, "PlayingIndicator") as TextBlock;
                if (indicator != null)
                {
                    indicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        // Show indicator for currently playing song
        if (_viewModel.CurrentPlayingQueueIndex >= 0)
        {
            var adjustedIndex = _viewModel.CurrentPlayingQueueIndex - (int)((_viewModel.QueuePageNumber - 1) * 20); // Account for paging
            System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] CurrentPlayingQueueIndex: {_viewModel.CurrentPlayingQueueIndex}, QueuePageNumber: {_viewModel.QueuePageNumber}, AdjustedIndex: {adjustedIndex}, Queue.Count: {_viewModel.Queue.Count}");
            
            if (adjustedIndex >= 0 && adjustedIndex < _viewModel.Queue.Count)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✓ Showing gold symbol at visual index {adjustedIndex}");
                var playingItem = QueueListView.ContainerFromIndex(adjustedIndex) as ListViewItem;
                
                // If container is not ready, try again after a short delay
                if (playingItem == null && retryCount < maxRetries)
                {
                    System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] Container not ready, retrying in 50ms... (attempt {retryCount + 1}/{maxRetries})");
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        RefreshQueueVisualStatesWithRetry(retryCount + 1);
                    });
                    return;
                }
                else if (playingItem == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✗ Container still not available after {maxRetries} retries");
                    return;
                }
                
                if (playingItem.ContentTemplateRoot is FrameworkElement root)
                {
                    var indicator = FindElementByName(root, "PlayingIndicator") as TextBlock;
                    if (indicator != null)
                    {
                        indicator.Visibility = Visibility.Visible;
                        System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✓ Gold symbol successfully placed at visual index {adjustedIndex}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✗ Could not find PlayingIndicator element at visual index {adjustedIndex}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✗ Could not get container or root element for visual index {adjustedIndex}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] ✗ AdjustedIndex {adjustedIndex} out of bounds (0-{_viewModel.Queue.Count - 1})");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[RefreshQueueVisualStates] No current playing song (index: {_viewModel.CurrentPlayingQueueIndex})");
        }
    }

    private static FrameworkElement? FindElementByName(DependencyObject parent, string name)
    {
        if (parent is FrameworkElement element && element.Name == name)
            return element;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindElementByName(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    private void OnWindowKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Check for Ctrl modifier
        var isCtrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        
        if (isCtrlPressed)
        {
            switch (e.Key)
            {
                case VirtualKey.Up:
                    OnMoveUpClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.Down:
                    OnMoveDownClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.Left:
                    OnMoveTopClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    OnMoveBottomClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.S:
                    OnSettingsClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.P:
                    OnPlayClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.N:
                    OnNextClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.R:
                    OnRepeatClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.F:
                    OnFullScreenClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case VirtualKey.Space:
                    OnPauseClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.Escape:
                    OnStopClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
            }
        }
    }
}