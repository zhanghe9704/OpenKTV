using System;
using Karaoke.Common.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Karaoke.UI.Views;

public sealed partial class SongDetailsDialog : ContentDialog
{
    public SongDetailsDialog(SongDto song)
    {
        Song = song ?? throw new ArgumentNullException(nameof(song));
        InitializeComponent();
    }

    public SongDto Song { get; }

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
}
