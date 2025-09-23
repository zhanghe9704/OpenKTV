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
            });
        }

        libraryNode["Roots"] = array;

        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(
            settingsPath,
            rootNode.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }),
            cancellationToken).ConfigureAwait(false);
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
        };
    }
}
