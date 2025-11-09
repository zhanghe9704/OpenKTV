using System;
using System.Threading;
using System.Threading.Tasks;
using Karaoke.Common.Models;
using Karaoke.Library.Services;
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
    private readonly ILibraryService _libraryService;
    private readonly DispatcherTimer _currentSongRefreshTimer;
    private readonly DispatcherTimer _artistsPageSizeUpdateTimer;
    private readonly DispatcherTimer _songsPageSizeUpdateTimer;
    private readonly DispatcherTimer _queuePageSizeUpdateTimer;

    public MainWindow(MainViewModel viewModel, LibrarySettingsViewModel settingsViewModel, IPlaybackService playbackService, ILibraryService libraryService)
    {
        InitializeComponent();
        Title = "OpenKTV";
        _viewModel = viewModel;
        _settingsViewModel = settingsViewModel;
        _playbackService = playbackService;
        _libraryService = libraryService;

        // Set initial volume from slider default value
        _ = _playbackService.SetVolumeAsync((int)VolumeSlider.Value, CancellationToken.None);

        _currentSongRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10) // Reduced frequency since we have event-driven updates
        };
        _currentSongRefreshTimer.Tick += OnCurrentSongRefreshTimer;

        // Initialize page size update timers with debouncing
        _artistsPageSizeUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _artistsPageSizeUpdateTimer.Tick += (s, e) =>
        {
            _artistsPageSizeUpdateTimer.Stop();
            UpdateArtistsPageSize();
        };

        _songsPageSizeUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _songsPageSizeUpdateTimer.Tick += (s, e) =>
        {
            _songsPageSizeUpdateTimer.Stop();
            UpdateSongsPageSize();
        };

        _queuePageSizeUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _queuePageSizeUpdateTimer.Tick += (s, e) =>
        {
            _queuePageSizeUpdateTimer.Stop();
            UpdateQueuePageSize();
        };

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
            // Ensure UI updates happen on the UI thread
            var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            if (dispatcherQueue != null)
            {
                dispatcherQueue.TryEnqueue(() => RefreshQueueVisualStates());
            }
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
        System.Diagnostics.Debug.WriteLine($"Dialog result: {result}, rescanArgs: {rescanArgs != null}");
        if (result == ContentDialogResult.Primary && rescanArgs != null)
        {
            System.Diagnostics.Debug.WriteLine($"RescanAll: {rescanArgs.RescanAll}, RootsCount: {rescanArgs.RootsToRescan.Count}");
            // Create progress UI elements
            var progressRing = new ProgressRing { IsActive = true, Width = 40, Height = 40 };
            var progressText = new TextBlock
            {
                Text = "Preparing to scan...",
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            var progressDetails = new TextBlock
            {
                Text = "",
                TextAlignment = TextAlignment.Center,
                FontSize = 14
            };

            // Show scanning progress dialog
            var progressDialog = new ContentDialog
            {
                Title = "Scanning Library",
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children = { progressRing, progressText, progressDetails }
                },
                XamlRoot = (Content as FrameworkElement)?.XamlRoot,
                DefaultButton = ContentDialogButton.None
            };

            // Start showing the dialog (don't await yet)
            var dialogTask = progressDialog.ShowAsync();

            try
            {
                // Give the UI a moment to render the dialog
                await Task.Delay(100);

                System.Diagnostics.Debug.WriteLine("Starting rescan...");

                // Create progress reporter that updates UI directly
                var progress = new Progress<Karaoke.Library.Ingestion.ScanProgress>(p =>
                {
                    System.Diagnostics.Debug.WriteLine($"Progress callback received: {p.RootName} - {p.FilesScanned} files");
                    // Update UI and force render
                    progressText.Text = $"Scanning: {p.RootName}";
                    progressDetails.Text = $"{p.FilesScanned} files scanned";
                    // Force UI to update immediately
                    progressText.UpdateLayout();
                    progressDetails.UpdateLayout();
                    System.Diagnostics.Debug.WriteLine($"UI Updated: {progressText.Text}");
                });

                // Perform the rescan
                if (rescanArgs.RescanAll)
                {
                    // Rescan all roots
                    await _viewModel.ReloadAsync(rescan: true, CancellationToken.None, progress);
                }
                else if (rescanArgs.RootsAddNewOnly.Count > 0 || rescanArgs.RootsToRescan.Count > 0)
                {
                    // First, handle AddNewOnly roots
                    var rootsAddNewOnly = rescanArgs.RootsAddNewOnly.ToList();
                    if (rootsAddNewOnly.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Scanning AddNewOnly roots: {string.Join(", ", rootsAddNewOnly)}");
                        await _viewModel.ReloadAddNewOnlyAsync(rootsAddNewOnly, CancellationToken.None, progress);
                    }

                    // Then, handle regular rescan roots (excluding AddNewOnly roots)
                    var rootsToFullRescan = rescanArgs.RootsToRescan.Except(rootsAddNewOnly).ToList();
                    if (rootsToFullRescan.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Scanning regular roots: {string.Join(", ", rootsToFullRescan)}");
                        await _viewModel.ReloadSpecificRootsAsync(rootsToFullRescan, CancellationToken.None, progress);
                    }
                }

                System.Diagnostics.Debug.WriteLine("Rescan completed");

                // Close the progress dialog
                progressDialog.Hide();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during rescan: {ex}");
                progressDialog.Hide();
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
            // Check if there are more songs in the queue
            var current = await _playbackService.GetCurrentAsync(CancellationToken.None).ConfigureAwait(false);
            if (current == null)
            {
                System.Diagnostics.Debug.WriteLine("Next: No current song");
                return;
            }

            // Check if this is the last song by attempting to peek at the queue
            // The queue in the viewmodel should reflect what's in the playback service
            var queueCount = _viewModel.Queue.Count;
            var currentPlayingIndex = _viewModel.CurrentPlayingQueueIndex;

            System.Diagnostics.Debug.WriteLine($"Next: Queue count={queueCount}, Current index={currentPlayingIndex}");

            // If current song is the last one in the queue, don't move next
            if (currentPlayingIndex >= 0 && currentPlayingIndex >= queueCount - 1)
            {
                System.Diagnostics.Debug.WriteLine("Next: Already on last song, ignoring");
                return;
            }

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

    private async void OnVocalClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            await _playbackService.ToggleVocalAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog to user
            System.Diagnostics.Debug.WriteLine($"Vocal toggle error: {ex.Message}");
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

    private void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        if (_viewModel.DeleteSelectedSongCommand.CanExecute(null))
        {
            _viewModel.DeleteSelectedSongCommand.Execute(null);
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
                case VirtualKey.D:
                    OnDeleteClicked(this, new RoutedEventArgs());
                    e.Handled = true;
                    break;
                case VirtualKey.Add: // Numpad +
                case (VirtualKey)187: // Regular + (same key as =)
                    OnVolumeIncrease();
                    e.Handled = true;
                    break;
                case VirtualKey.Subtract: // Numpad -
                case (VirtualKey)189: // Regular -
                    OnVolumeDecrease();
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

    private void OnSongRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // The context menu will automatically show
        // This handler ensures the right-clicked item is selected
        if (e.OriginalSource is FrameworkElement element)
        {
            var song = element.DataContext as SongDto;
            if (song != null)
            {
                _viewModel.SelectedSong = song;
            }
        }
    }

    private async void OnViewSongDetails(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedSong == null)
        {
            return;
        }

        try
        {
            var dialog = new SongDetailsDialog(_viewModel.SelectedSong, _libraryService)
            {
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();

            // Only reload if changes were actually saved
            if (dialog.HasUnsavedChanges)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Song details were modified, reloading with state preservation...");
                await _viewModel.ReloadWithStatePreservationAsync(CancellationToken.None);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Song details dialog closed without changes, no reload needed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error showing song details: {ex}");
        }
    }

    private async void OnQueueRightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        // This handler ensures the right-clicked item is selected
        // and only shows the context menu if the song is currently playing
        if (e.OriginalSource is FrameworkElement element)
        {
            var song = element.DataContext as SongDto;
            if (song != null)
            {
                _viewModel.SelectedQueuedSong = song;

                // Check if this song is currently playing
                var currentSong = await _viewModel.GetCurrentSongAsync();
                if (currentSong != null && currentSong.Id == song.Id)
                {
                    // This is the currently playing song, show the context menu
                    var flyout = new MenuFlyout();
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = "Set Track as Default",
                        Icon = new SymbolIcon(Symbol.Pin)
                    };
                    menuItem.Click += OnSetTrackAsDefault;
                    flyout.Items.Add(menuItem);

                    flyout.ShowAt(element, e.GetPosition(element));
                }
            }
        }
    }

    private async void OnSetTrackAsDefault(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedQueuedSong == null)
        {
            return;
        }

        try
        {
            await _viewModel.SetTrackAsDefaultAsync(_viewModel.SelectedQueuedSong);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting track as default: {ex}");
        }
    }

    private async void OnVolumeChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        // Guard against initialization - slider ValueChanged fires before constructor completes
        if (_playbackService == null)
        {
            return;
        }

        try
        {
            var volume = (int)e.NewValue;
            await _playbackService.SetVolumeAsync(volume, CancellationToken.None);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing volume: {ex}");
        }
    }

    private async void OnVolumeIncrease()
    {
        if (_playbackService == null)
        {
            return;
        }

        try
        {
            var currentVolume = await _playbackService.GetVolumeAsync(CancellationToken.None);
            var newVolume = Math.Min(100, currentVolume + 5);
            VolumeSlider.Value = newVolume;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error increasing volume: {ex}");
        }
    }

    private async void OnVolumeDecrease()
    {
        if (_playbackService == null)
        {
            return;
        }

        try
        {
            var currentVolume = await _playbackService.GetVolumeAsync(CancellationToken.None);
            var newVolume = Math.Max(0, currentVolume - 5);
            VolumeSlider.Value = newVolume;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error decreasing volume: {ex}");
        }
    }

    private async void OnVolumeNormChanged(object sender, RoutedEventArgs e)
    {
        if (_playbackService == null)
        {
            return;
        }

        try
        {
            var isEnabled = VolumeNormCheckBox.IsChecked ?? false;
            await _playbackService.SetVolumeNormalizationAsync(isEnabled, CancellationToken.None);
            System.Diagnostics.Debug.WriteLine($"Volume normalization {(isEnabled ? "enabled" : "disabled")}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error changing volume normalization: {ex}");
        }
    }

    private void OnArtistsListViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Debounce the update to avoid reentrancy issues
        _artistsPageSizeUpdateTimer.Stop();
        _artistsPageSizeUpdateTimer.Start();
    }

    private void OnSongsListViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Debounce the update to avoid reentrancy issues
        _songsPageSizeUpdateTimer.Stop();
        _songsPageSizeUpdateTimer.Start();
    }

    private void OnQueueListViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Debounce the update to avoid reentrancy issues
        _queuePageSizeUpdateTimer.Stop();
        _queuePageSizeUpdateTimer.Start();
    }

    private void UpdateArtistsPageSize()
    {
        if (ArtistsListView == null || _viewModel == null) return;

        var availableHeight = ArtistsListView.ActualHeight;
        if (availableHeight <= 0) return;

        // Estimate item height: simple text item is approximately 42 pixels
        const double estimatedItemHeight = 42.0;
        var itemsPerPage = (int)Math.Ceiling(availableHeight / estimatedItemHeight);

        // Ensure at least 1 item per page
        itemsPerPage = Math.Max(1, itemsPerPage);

        _viewModel.SetArtistsPageSize(itemsPerPage);
    }

    private void UpdateSongsPageSize()
    {
        if (SongsListView == null || _viewModel == null) return;

        var availableHeight = SongsListView.ActualHeight;
        if (availableHeight <= 0) return;

        // Estimate item height: songs have 2 lines (title/details + path), approximately 60 pixels
        const double estimatedItemHeight = 60.0;
        var itemsPerPage = (int)Math.Ceiling(availableHeight / estimatedItemHeight);

        // Ensure at least 1 item per page
        itemsPerPage = Math.Max(1, itemsPerPage);

        _viewModel.SetSongsPageSize(itemsPerPage);
    }

    private void UpdateQueuePageSize()
    {
        if (QueueListView == null || _viewModel == null) return;

        var availableHeight = QueueListView.ActualHeight;
        if (availableHeight <= 0) return;

        // Estimate item height: queue items have 2 lines (title + artist), approximately 58-62 pixels
        const double estimatedItemHeight = 60.0;
        var itemsPerPage = (int)Math.Ceiling(availableHeight / estimatedItemHeight);

        // Ensure at least 1 item per page
        itemsPerPage = Math.Max(1, itemsPerPage);

        _viewModel.SetQueuePageSize(itemsPerPage);
    }

}