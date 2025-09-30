using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Karaoke.UI.ViewModels.Settings;

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
}