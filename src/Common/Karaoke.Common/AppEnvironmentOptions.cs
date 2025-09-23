namespace Karaoke.Common;

public sealed class AppEnvironmentOptions
{
    public const string SectionName = "AppEnvironment";

    public string EnvironmentName { get; set; } = "Development";

    public string ConfigurationRoot { get; set; } = "config";

    public string AssetsRoot { get; set; } = "assets";
}
