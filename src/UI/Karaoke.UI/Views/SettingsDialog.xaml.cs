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
            await ViewModel.SaveAsync().ConfigureAwait(true);
            args.Cancel = false;
        }
        catch (IOException ex)
        {
            args.Cancel = true;
            HandleSaveError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            args.Cancel = true;
            HandleSaveError(ex);
        }
        catch (InvalidOperationException ex)
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
    }
}