using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Karaoke.UI.ViewModels.Settings;

public partial class LibraryRootItemViewModel : ObservableObject
{
    public LibraryRootItemViewModel(string name, string path, int? defaultPriority, string? defaultChannel, string? driveOverride, string? keywordFormat = null, int instrumental = 0, bool shouldRescan = true, bool volumeNormalization = false)
    {
        OriginalName = name;
        _name = name;
        _path = path;
        _defaultPriority = (defaultPriority ?? 2).ToString(CultureInfo.InvariantCulture);
        _defaultChannel = defaultChannel ?? "Stereo";
        _driveOverride = driveOverride;
        _keywordFormat = keywordFormat;
        _instrumental = instrumental.ToString(CultureInfo.InvariantCulture);
        _shouldRescan = shouldRescan;
        _volumeNormalization = volumeNormalization;
    }

    public string OriginalName { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _path;

    [ObservableProperty]
    private string _defaultPriority;

    [ObservableProperty]
    private string _defaultChannel;

    [ObservableProperty]
    private string? _driveOverride;

    [ObservableProperty]
    private string? _keywordFormat;

    [ObservableProperty]
    private string _instrumental;

    [ObservableProperty]
    private bool _shouldRescan;

    [ObservableProperty]
    private bool _volumeNormalization;

    public int GetPriority()
    {
        return int.TryParse(DefaultPriority, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 2;
    }

    public int GetInstrumental()
    {
        return int.TryParse(Instrumental, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;
    }
}
