using System;
using System.Diagnostics;
using Karaoke.UI.ViewModels.Settings;
using Microsoft.UI.Xaml.Controls;

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
        args.Cancel = true;
        try
        {
            await ViewModel.SaveAsync();
            args.Cancel = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Settings save failed: {ex}");
            args.Cancel = true;
        }
    }
}
