using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karaoke.Common;
using Karaoke.Library.Configuration;
using Karaoke.Library.Ingestion;
using Karaoke.Library.Services;
using Karaoke.Library.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karaoke.Library.Tests;

public class LibraryIngestionServiceTests
{
    [Fact]
    public async Task ScanAsyncImportsSongsFromSampleAssets()
    {
        var repositoryRoot = GetRepositoryRoot();
        Assert.True(Directory.Exists(Path.Combine(repositoryRoot, "assets", "sample", "libraryA")));

        var databasePath = Path.Combine(Path.GetTempPath(), $"karaoke-tests-{Guid.NewGuid():N}.db");
        var libraryOptions = new LibraryOptions
        {
            DefaultChannel = "Stereo",
            DefaultPriority = 2,
            Roots = new List<LibraryRootOptions>
            {
                new() { Name = "SampleLibraryA", Path = "assets/sample/libraryA", DefaultPriority = 2 },
                new() { Name = "SampleLibraryB", Path = "assets/sample/libraryB", DefaultPriority = 1 },
            },
            DatabasePath = databasePath,
        };

        var options = Options.Create(libraryOptions);
        var repository = new SqliteLibraryRepository(options, NullLogger<SqliteLibraryRepository>.Instance);
        var libraryService = new LibraryService(repository, NullLogger<LibraryService>.Instance, new StaticOptionsMonitor(libraryOptions), new TestAppEnvironment(repositoryRoot));
        var ingestionService = CreateIngestionService(repositoryRoot, options, repository, libraryService: libraryService);

        try
        {
            var result = await ingestionService.ScanAsync(CancellationToken.None);

            Assert.Equal(4, result.FilesProcessed);
            Assert.Equal(0, result.FilesSkipped);

            var songs = await libraryService.GetAllSongsAsync(CancellationToken.None);
            Assert.Equal(4, songs.Count);
            Assert.Contains(songs, song => song.Id == "SampleLibraryA:artist one/song one.mp3" && song.Artist == "Artist One" && song.Title == "Song One");
            Assert.Contains(songs, song => song.Id == "SampleLibraryB:artist two-song three.mp3" && song.Artist == "Artist Two" && song.Title == "Song Three");
        }
        finally
        {
            repository.Dispose();
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task ScanAsyncSanitizesMetadata()
    {
        var tempDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var filePath = Path.Combine(tempDirectory.FullName, "temp.mp3");
        await File.WriteAllTextAsync(filePath, string.Empty);

        SqliteLibraryRepository? repository = null;
        try
        {
            var libraryOptions = new LibraryOptions
            {
                DefaultChannel = "Stereo",
                DefaultPriority = 2,
                Roots = new List<LibraryRootOptions>
                {
                    new() { Name = "Temp", Path = tempDirectory.FullName }
                },
                DatabasePath = Path.Combine(tempDirectory.FullName, "library.db"),
            };

            var options = Options.Create(libraryOptions);
            repository = new SqliteLibraryRepository(options, NullLogger<SqliteLibraryRepository>.Instance);
            var libraryService = new LibraryService(repository, NullLogger<LibraryService>.Instance, new StaticOptionsMonitor(libraryOptions), new TestAppEnvironment(tempDirectory.FullName));
            var ingestionService = CreateIngestionService(
                tempDirectory.FullName,
                options,
                repository,
                parsers: new[] { new SanitizingParser() },
                libraryService: libraryService);

            var result = await ingestionService.ScanAsync(CancellationToken.None);
            Assert.Equal(1, result.FilesProcessed);

            var songs = await libraryService.GetAllSongsAsync(CancellationToken.None);
            var song = Assert.Single(songs);
            Assert.Equal("Sanitized Title", song.Title);
            Assert.Equal("alert('x')", song.Artist);
        }
        finally
        {
            repository?.Dispose();
            SqliteConnection.ClearAllPools();
            Directory.Delete(tempDirectory.FullName, recursive: true);
        }
    }

    private static LibraryIngestionService CreateIngestionService(
        string contentRoot,
        IOptions<LibraryOptions> options,
        ILibraryRepository repository,
        IEnumerable<IMediaPathParser>? parsers = null,
        LibraryService? libraryService = null)
    {
        parsers ??= new IMediaPathParser[]
        {
            new DirectoryStructureParser(),
            new HyphenFileNameParser(),
        };

        var appEnvironment = new TestAppEnvironment(contentRoot);
        libraryService ??= new LibraryService(repository, NullLogger<LibraryService>.Instance, new StaticOptionsMonitor(options.Value), appEnvironment);

        return new LibraryIngestionService(
            parsers,
            libraryService,
            repository,
            options,
            appEnvironment,
            NullLogger<LibraryIngestionService>.Instance);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            ".."));
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<LibraryOptions>
    {
        public StaticOptionsMonitor(LibraryOptions value)
        {
            CurrentValue = value;
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

    private sealed class TestAppEnvironment(string rootPath) : IAppEnvironment
    {
        public string EnvironmentName => "Test";

        public string ApplicationRootPath => rootPath;

        public string ConfigurationRootPath => Path.Combine(rootPath, "config");

        public string AssetsRootPath => Path.Combine(rootPath, "assets");
    }

    private sealed class SanitizingParser : IMediaPathParser
    {
        public bool TryParse(MediaFileContext context, out ParsedSongMetadata metadata)
        {
            metadata = new ParsedSongMetadata(
                "temp.mp3",
                "<script>Sanitized Title</script>\r\n",
                "<b>alert('x')</b>",
                "Stereo",
                2);
            return true;
        }
    }
}
