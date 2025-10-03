using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Karaoke.UI.ViewModels.Settings;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Karaoke.UI.Views;

public sealed partial class SettingsDialog : ContentDialog
{
    public SettingsDialog(LibrarySettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = ViewModel;
        PrimaryButtonClick += OnPrimaryButtonClick;
    }

    public LibrarySettingsViewModel ViewModel { get; }

    private void OnAddTabButtonClick(TabView sender, object args)
    {
        ViewModel.AddRootCommand.Execute(null);
    }

    private void OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is LibraryRootItemViewModel root)
        {
            ViewModel.SelectedRoot = root;
            ViewModel.RemoveSelectedRootCommand.Execute(null);
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        
        try
        {
            if (ViewModel == null)
            {
                args.Cancel = true;
                return;
            }
            
            await ViewModel.SaveAsync().ConfigureAwait(true);
            args.Cancel = false;
        }
        catch (Exception ex)
        {
            args.Cancel = true;
            HandleSaveError(ex);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private static void HandleSaveError(Exception ex)
    {
        Debug.WriteLine($"Settings save failed: {ex}");

        try
        {
            // Instead of showing another dialog, just close the current dialog and let the user know via debug/console
            System.Console.WriteLine($"Save failed: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
        }
        catch (Exception dialogEx)
        {
            // If anything goes wrong, just log it
            System.Console.WriteLine($"Save error: {ex}");
            System.Diagnostics.Debug.WriteLine($"Failed to handle error: {dialogEx}");
        }
    }

    private async void OnBrowseFolderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not LibraryRootItemViewModel rootViewModel)
        {
            return;
        }

        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            ViewMode = PickerViewMode.List
        };
        folderPicker.FileTypeFilter.Add("*");

        // Initialize the picker with the window handle
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            rootViewModel.Path = folder.Path;
        }
    }
}