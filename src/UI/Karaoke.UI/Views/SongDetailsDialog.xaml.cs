using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Karaoke.Common.Models;
using Karaoke.Library.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Karaoke.UI.Views;

public sealed partial class SongDetailsDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly ILibraryService _libraryService;
    private bool _isEditMode;
    private string _editedTitle;
    private string _editedArtist;
    private string? _editedLanguage;
    private string? _editedGenre;
    private string? _editedComment;
    private double _editedPriority;
    private double _editedInstrumental;
    private bool _showStatusMessage;
    private InfoBarSeverity _statusSeverity;
    private string _statusTitle = string.Empty;
    private string _statusMessage = string.Empty;

    public SongDetailsDialog(SongDto song, ILibraryService libraryService)
    {
        Song = song ?? throw new ArgumentNullException(nameof(song));
        _libraryService = libraryService ?? throw new ArgumentNullException(nameof(libraryService));

        // Initialize edited values with current song values
        _editedTitle = song.Title;
        _editedArtist = song.Artist;
        _editedLanguage = song.Language;
        _editedGenre = song.Genre;
        _editedComment = song.Comment;
        _editedPriority = song.Priority;
        _editedInstrumental = song.Instrumental;

        InitializeComponent();
    }

    public SongDto Song { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode != value)
            {
                _isEditMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsViewMode));
                OnPropertyChanged(nameof(PrimaryButtonText));
                OnPropertyChanged(nameof(SecondaryButtonText));
            }
        }
    }

    public Visibility IsViewMode => IsEditMode ? Visibility.Collapsed : Visibility.Visible;

    public new string PrimaryButtonText => IsEditMode ? "Save" : "Edit";

    public new string SecondaryButtonText => IsEditMode ? "Cancel" : "";

    public string EditedTitle
    {
        get => _editedTitle;
        set
        {
            if (_editedTitle != value)
            {
                _editedTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public string EditedArtist
    {
        get => _editedArtist;
        set
        {
            if (_editedArtist != value)
            {
                _editedArtist = value;
                OnPropertyChanged();
            }
        }
    }

    public string? EditedLanguage
    {
        get => _editedLanguage;
        set
        {
            if (_editedLanguage != value)
            {
                _editedLanguage = value;
                OnPropertyChanged();
            }
        }
    }

    public string? EditedGenre
    {
        get => _editedGenre;
        set
        {
            if (_editedGenre != value)
            {
                _editedGenre = value;
                OnPropertyChanged();
            }
        }
    }

    public string? EditedComment
    {
        get => _editedComment;
        set
        {
            if (_editedComment != value)
            {
                _editedComment = value;
                OnPropertyChanged();
            }
        }
    }

    public double EditedPriority
    {
        get => _editedPriority;
        set
        {
            if (Math.Abs(_editedPriority - value) > 0.001)
            {
                _editedPriority = value;
                OnPropertyChanged();
            }
        }
    }

    public double EditedInstrumental
    {
        get => _editedInstrumental;
        set
        {
            if (Math.Abs(_editedInstrumental - value) > 0.001)
            {
                _editedInstrumental = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowStatusMessage
    {
        get => _showStatusMessage;
        set
        {
            if (_showStatusMessage != value)
            {
                _showStatusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public InfoBarSeverity StatusSeverity
    {
        get => _statusSeverity;
        set
        {
            if (_statusSeverity != value)
            {
                _statusSeverity = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusTitle
    {
        get => _statusTitle;
        set
        {
            if (_statusTitle != value)
            {
                _statusTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public string LoudnessText => Song.LoudnessLufs.HasValue
        ? $"{Song.LoudnessLufs.Value:F1}"
        : "Not analyzed";

    public string GainText => Song.GainDb.HasValue
        ? $"{Song.GainDb.Value:+0.0;-0.0;0.0}"
        : "N/A";

    public string NormalizationStatus
    {
        get
        {
            if (!Song.LoudnessLufs.HasValue || !Song.GainDb.HasValue)
            {
                return "Not normalized - Volume normalization not performed";
            }

            if (Math.Abs(Song.LoudnessLufs.Value - (-14.0)) < 0.5)
            {
                return "Already normalized - No adjustment needed";
            }

            return "Normalized - Gain adjustment will be applied during playback";
        }
    }

    public SolidColorBrush NormalizationStatusColor
    {
        get
        {
            if (!Song.LoudnessLufs.HasValue || !Song.GainDb.HasValue)
            {
                return new SolidColorBrush(Colors.Orange);
            }

            if (Math.Abs(Song.LoudnessLufs.Value - (-14.0)) < 0.5)
            {
                return new SolidColorBrush(Colors.LightGreen);
            }

            return new SolidColorBrush(Colors.LightBlue);
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (!IsEditMode)
        {
            // Switch to edit mode
            IsEditMode = true;
            args.Cancel = true; // Prevent dialog from closing
        }
        else
        {
            // Save changes
            var deferral = args.GetDeferral();
            args.Cancel = true; // Always prevent dialog from closing - only Close button should close
            try
            {
                if (!ValidateInput())
                {
                    return;
                }

                // Create updated song DTO
                var updatedSong = Song with
                {
                    Title = EditedTitle.Trim(),
                    Artist = EditedArtist.Trim(),
                    Language = string.IsNullOrWhiteSpace(EditedLanguage) ? null : EditedLanguage.Trim(),
                    Genre = string.IsNullOrWhiteSpace(EditedGenre) ? null : EditedGenre.Trim(),
                    Comment = string.IsNullOrWhiteSpace(EditedComment) ? null : EditedComment.Trim(),
                    Priority = (int)EditedPriority,
                    Instrumental = (int)EditedInstrumental
                };

                System.Diagnostics.Debug.WriteLine($"[SongDetailsDialog] Saving song: {updatedSong.Id}");
                System.Diagnostics.Debug.WriteLine($"[SongDetailsDialog] Title: '{updatedSong.Title}', Artist: '{updatedSong.Artist}'");
                System.Diagnostics.Debug.WriteLine($"[SongDetailsDialog] Priority: {updatedSong.Priority}, Instrumental: {updatedSong.Instrumental}");

                await _libraryService.UpsertAsync(updatedSong, System.Threading.CancellationToken.None);

                System.Diagnostics.Debug.WriteLine($"[SongDetailsDialog] Save completed successfully");

                // Show success message in InfoBar
                ShowStatusInfo("Success", "Changes saved successfully!", InfoBarSeverity.Success);

                // Stay in edit mode so user can continue editing or click Close to exit
            }
            catch (Exception ex)
            {
                ShowStatusInfo("Error", $"Failed to save changes: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }

    private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (IsEditMode)
        {
            // Cancel edit mode - revert to original values
            EditedTitle = Song.Title;
            EditedArtist = Song.Artist;
            EditedLanguage = Song.Language;
            EditedGenre = Song.Genre;
            EditedComment = Song.Comment;
            EditedPriority = Song.Priority;
            EditedInstrumental = Song.Instrumental;

            IsEditMode = false;
            args.Cancel = true; // Prevent dialog from closing
        }
    }

    private bool ValidateInput()
    {
        // Title and Artist are required
        if (string.IsNullOrWhiteSpace(EditedTitle))
        {
            ShowStatusInfo("Validation Error", "Title cannot be empty.", InfoBarSeverity.Warning);
            return false;
        }

        if (string.IsNullOrWhiteSpace(EditedArtist))
        {
            ShowStatusInfo("Validation Error", "Artist cannot be empty.", InfoBarSeverity.Warning);
            return false;
        }

        // Priority should be between 1 and 10
        if (EditedPriority < 1 || EditedPriority > 10)
        {
            ShowStatusInfo("Validation Error", "Priority must be between 1 and 10.", InfoBarSeverity.Warning);
            return false;
        }

        // Instrumental should be 0 or 1
        if (EditedInstrumental < 0 || EditedInstrumental > 1 || EditedInstrumental != Math.Floor(EditedInstrumental))
        {
            ShowStatusInfo("Validation Error", "Channel/Track must be 0 (Vocal) or 1 (Instrumental).", InfoBarSeverity.Warning);
            return false;
        }

        return true;
    }

    private void ShowStatusInfo(string title, string message, InfoBarSeverity severity)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
        ShowStatusMessage = true;
    }

    private void OnStatusInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        ShowStatusMessage = false;
    }
}
