using System.Linq;
using System.Text.Json;
using Karaoke.Common;
using Karaoke.Library.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karaoke.Library.Tests;

public class LibraryConfigurationManagerTests
{
    [Fact]
    public async Task SaveRootsAsyncPersistsChanges()
    {
        using var tempDirectory = new TempDirectory();
        var configPath = Path.Combine(tempDirectory.Path, "settings.json");
        var initialJson = "{\n  \"Library\": {\n    \"DefaultPriority\": 2,\n    \"DefaultChannel\": \"Stereo\",\n    \"DatabasePath\": \"data/library.db\",\n    \"Roots\": [\n      {\"Name\": \"SampleLibraryA\", \"Path\": \"assets/sample/libraryA\"}\n    ]\n  },\n  \"Logging\": { \"LogLevel\": { \"Default\": \"Information\" } }\n}";
        await File.WriteAllTextAsync(configPath, initialJson);

        var env = new TestAppEnvironment(tempDirectory.Path, tempDirectory.Path, tempDirectory.Path);
        var options = new LibraryOptions
        {
            DatabasePath = "data/library.db",
            Roots = new List<LibraryRootOptions>
            {
                new() { Name = "SampleLibraryA", Path = "assets/sample/libraryA" }
            }
        };

        var monitor = new StaticOptionsMonitor(options);
        var manager = new JsonLibraryConfigurationManager(monitor, env, NullLogger<JsonLibraryConfigurationManager>.Instance);
        var roots = new[]
        {
            new LibraryRootOptions { Name = "SampleLibraryA", Path = "assets/sample/libraryA", DriveOverride = "E:" },
            new LibraryRootOptions { Name = "SampleLibraryB", Path = "assets/sample/libraryB" },
        };

        await manager.SaveRootsAsync(roots, CancellationToken.None);

        var content = await File.ReadAllTextAsync(configPath);
        using var document = JsonDocument.Parse(content);
        var library = document.RootElement.GetProperty("Library");
        var rootsArray = library.GetProperty("Roots");
        Assert.Equal(2, rootsArray.GetArrayLength());
        var primaryRoot = rootsArray.EnumerateArray().First(element => element.GetProperty("Name").GetString() == "SampleLibraryA");
        Assert.Equal("E:", primaryRoot.GetProperty("DriveOverride").GetString());

        Assert.True(document.RootElement.TryGetProperty("Logging", out _));
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<LibraryOptions>
    {
        public StaticOptionsMonitor(LibraryOptions options)
        {
            CurrentValue = options;
        }

        public LibraryOptions CurrentValue { get; }

        public LibraryOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<LibraryOptions, string?> listener) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class TestAppEnvironment(string configurationRoot, string applicationRoot, string assetsRoot) : IAppEnvironment
    {
        public string EnvironmentName => "Test";

        public string ApplicationRootPath => applicationRoot;

        public string ConfigurationRootPath => configurationRoot;

        public string AssetsRootPath => assetsRoot;
    }
}
