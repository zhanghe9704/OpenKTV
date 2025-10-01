using System.Text.Json;
using System.Text.Json.Nodes;
using Karaoke.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karaoke.Library.Configuration;

public sealed class JsonLibraryConfigurationManager : ILibraryConfigurationManager
{
    private readonly IOptionsMonitor<LibraryOptions> _options;
    private readonly IAppEnvironment _appEnvironment;
    private readonly ILogger<JsonLibraryConfigurationManager> _logger;

    public JsonLibraryConfigurationManager(
        IOptionsMonitor<LibraryOptions> options,
        IAppEnvironment appEnvironment,
        ILogger<JsonLibraryConfigurationManager> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(appEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _appEnvironment = appEnvironment;
        _logger = logger;
    }

    public Task<IReadOnlyList<LibraryRootOptions>> GetRootsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var roots = _options.CurrentValue.Roots ?? new List<LibraryRootOptions>();
        var cloned = roots.Select(CloneRoot).ToList();
        return Task.FromResult<IReadOnlyList<LibraryRootOptions>>(cloned);
    }

    public Task<LibraryOptions> GetLibraryOptionsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = _options.CurrentValue;
        var cloned = new LibraryOptions
        {
            DefaultPriority = current.DefaultPriority,
            DefaultChannel = current.DefaultChannel,
            DatabasePath = current.DatabasePath,
            SupportedExtensions = new List<string>(current.SupportedExtensions),
            Roots = current.Roots.Select(CloneRoot).ToList(),
            KeywordFormat = current.KeywordFormat,
            DisplayOptions = new SongDisplayOptions
            {
                ShowArtist = current.DisplayOptions.ShowArtist,
                ShowLanguage = current.DisplayOptions.ShowLanguage,
                ShowGenre = current.DisplayOptions.ShowGenre,
                ShowComment = current.DisplayOptions.ShowComment,
                ShowChannel = current.DisplayOptions.ShowChannel,
                ShowPriority = current.DisplayOptions.ShowPriority
            }
        };
        return Task.FromResult(cloned);
    }

    public async Task SaveRootsAsync(IEnumerable<LibraryRootOptions> roots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roots);
        cancellationToken.ThrowIfCancellationRequested();

        var settingsPath = GetSettingsPath();
        JsonNode rootNode;
        if (File.Exists(settingsPath))
        {
            var content = await File.ReadAllTextAsync(settingsPath, cancellationToken).ConfigureAwait(false);
            rootNode = JsonNode.Parse(content) ?? new JsonObject();
        }
        else
        {
            rootNode = new JsonObject();
        }

        var libraryNode = rootNode["Library"] as JsonObject ?? new JsonObject();
        rootNode["Library"] = libraryNode;

        var array = new JsonArray();
        foreach (var option in roots)
        {
            array.Add(new JsonObject
            {
                ["Name"] = option.Name,
                ["Path"] = option.Path,
                ["DefaultPriority"] = option.DefaultPriority,
                ["DefaultChannel"] = option.DefaultChannel,
                ["DriveOverride"] = option.DriveOverride,
                ["KeywordFormat"] = option.KeywordFormat,
                ["Instrumental"] = option.Instrumental,
            });
        }

        libraryNode["Roots"] = array;

        await File.WriteAllTextAsync(
            settingsPath,
            rootNode.ToString(),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveLibraryOptionsAsync(LibraryOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var settingsPath = GetSettingsPath();
        System.Diagnostics.Debug.WriteLine($"[JsonLibraryConfigurationManager] Saving library options to {settingsPath}");
        System.Diagnostics.Debug.WriteLine($"[JsonLibraryConfigurationManager] Saving {options.Roots.Count} roots");
        
        JsonNode rootNode;
        if (File.Exists(settingsPath))
        {
            var content = await File.ReadAllTextAsync(settingsPath, cancellationToken).ConfigureAwait(false);
            rootNode = JsonNode.Parse(content) ?? new JsonObject();
        }
        else
        {
            rootNode = new JsonObject();
        }

        var libraryNode = rootNode["Library"] as JsonObject ?? new JsonObject();
        rootNode["Library"] = libraryNode;

        // Update global settings
        libraryNode["DefaultPriority"] = options.DefaultPriority;
        libraryNode["DefaultChannel"] = options.DefaultChannel;
        libraryNode["DatabasePath"] = options.DatabasePath;
        libraryNode["KeywordFormat"] = options.KeywordFormat;

        // Update roots
        var rootsArray = new JsonArray();
        foreach (var root in options.Roots)
        {
            System.Diagnostics.Debug.WriteLine($"[JsonLibraryConfigurationManager] Root '{root.Name}': Instrumental = {root.Instrumental}");
            rootsArray.Add(new JsonObject
            {
                ["Name"] = root.Name,
                ["Path"] = root.Path,
                ["DefaultPriority"] = root.DefaultPriority,
                ["DefaultChannel"] = root.DefaultChannel,
                ["DriveOverride"] = root.DriveOverride,
                ["KeywordFormat"] = root.KeywordFormat,
                ["Instrumental"] = root.Instrumental,
            });
        }
        libraryNode["Roots"] = rootsArray;

        // Update supported extensions
        var extensionsArray = new JsonArray();
        foreach (var extension in options.SupportedExtensions)
        {
            extensionsArray.Add(extension);
        }
        libraryNode["SupportedExtensions"] = extensionsArray;

        // Update display options
        var displayOptionsNode = new JsonObject
        {
            ["ShowArtist"] = options.DisplayOptions.ShowArtist,
            ["ShowLanguage"] = options.DisplayOptions.ShowLanguage,
            ["ShowGenre"] = options.DisplayOptions.ShowGenre,
            ["ShowComment"] = options.DisplayOptions.ShowComment,
            ["ShowChannel"] = options.DisplayOptions.ShowChannel,
            ["ShowPriority"] = options.DisplayOptions.ShowPriority
        };
        libraryNode["DisplayOptions"] = displayOptionsNode;

        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);

        // Use JsonSerializer.Serialize for proper formatting instead of ToJsonString
        var jsonString = JsonSerializer.Serialize(rootNode, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        System.Diagnostics.Debug.WriteLine($"[JsonLibraryConfigurationManager] Writing to file: {settingsPath}");
        await File.WriteAllTextAsync(settingsPath, jsonString, cancellationToken).ConfigureAwait(false);
        System.Diagnostics.Debug.WriteLine($"[JsonLibraryConfigurationManager] File saved successfully");
    }

    private string GetSettingsPath()
    {
        return Path.Combine(_appEnvironment.ConfigurationRootPath, "settings.json");
    }

    private static LibraryRootOptions CloneRoot(LibraryRootOptions source)
    {
        return new LibraryRootOptions
        {
            Name = source.Name,
            Path = source.Path,
            DefaultPriority = source.DefaultPriority,
            DefaultChannel = source.DefaultChannel,
            DriveOverride = source.DriveOverride,
            KeywordFormat = source.KeywordFormat,
            Instrumental = source.Instrumental,
        };
    }
}
