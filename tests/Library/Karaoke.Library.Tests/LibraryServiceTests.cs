using System.Collections.Generic;
using System.IO;
using System.Linq;
using Karaoke.Common;
using Karaoke.Common.Models;
using Karaoke.Library.Configuration;
using Karaoke.Library.Services;
using Karaoke.Library.Storage;
using Karaoke.Library.Storage.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karaoke.Library.Tests;

public class LibraryServiceTests
{
    private static SongDto CreateSong(string artist, string title, int priority = 2)
    {
        var relativePath = $"{artist}/{title}.mp3";
        var songId = $"Root:{relativePath}";
        return new SongDto(songId, title, artist, $"assets/{relativePath}", "Stereo", priority);
    }

    [Fact]
    public async Task SearchAsyncFiltersByArtistOrTitle()
    {
        var repository = new InMemoryLibraryRepository();
        var logger = NullLogger<LibraryService>.Instance;
        var options = new LibraryOptions
        {
            Roots = new List<LibraryRootOptions>
            {
                new() { Name = "Root", Path = "assets/sample" },
            }
        };
        var service = new LibraryService(repository, logger, new StaticOptionsMonitor(options), new TestAppEnvironment());
        await service.UpsertAsync(CreateSong("Artist A", "Song A"), CancellationToken.None);
        await service.UpsertAsync(CreateSong("Artist B", "Song B"), CancellationToken.None);

        var results = await service.SearchAsync("Artist B", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Root:Artist B/Song B.mp3", results[0].Id);
    }

    [Fact]
    public async Task GetAllSongsAsyncOrdersByPriorityArtistAndTitle()
    {
        var repository = new InMemoryLibraryRepository();
        var logger = NullLogger<LibraryService>.Instance;
        var options = new LibraryOptions
        {
            Roots = new List<LibraryRootOptions>
            {
                new() { Name = "Root", Path = "assets/sample" },
            }
        };
        var service = new LibraryService(repository, logger, new StaticOptionsMonitor(options), new TestAppEnvironment());
        await service.UpsertAsync(CreateSong("Zeta", "Alpha", priority: 2), CancellationToken.None);
        await service.UpsertAsync(CreateSong("Alpha", "Beta", priority: 1), CancellationToken.None);
        await service.UpsertAsync(CreateSong("Alpha", "Gamma", priority: 1), CancellationToken.None);

        var results = await service.GetAllSongsAsync(CancellationToken.None);

        Assert.Collection(
            results,
            song => Assert.Equal("Root:Alpha/Beta.mp3", song.Id),
            song => Assert.Equal("Root:Alpha/Gamma.mp3", song.Id),
            song => Assert.Equal("Root:Zeta/Alpha.mp3", song.Id));
    }

    private sealed class InMemoryLibraryRepository : ILibraryRepository
    {
        private readonly Dictionary<string, SongRecord> _songs = new(StringComparer.OrdinalIgnoreCase);

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<SongRecord>> GetSongsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<SongRecord>>(_songs.Values.ToList());
        }

        public Task UpsertSongAsync(SongRecord song, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _songs[song.Id] = song;
            return Task.CompletedTask;
        }

        public Task DeleteAllSongsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _songs.Clear();
            return Task.CompletedTask;
        }
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

    private sealed class TestAppEnvironment : IAppEnvironment
    {
        public string EnvironmentName => "Test";

        public string ApplicationRootPath => Directory.GetCurrentDirectory();

        public string ConfigurationRootPath => Directory.GetCurrentDirectory();

        public string AssetsRootPath => Path.Combine(Directory.GetCurrentDirectory(), "assets");
    }
}
