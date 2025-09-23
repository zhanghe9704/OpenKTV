using System.Data;
using System.Globalization;
using Karaoke.Library.Configuration;
using Karaoke.Library.Storage.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Karaoke.Library.Storage;

public sealed class SqliteLibraryRepository : ILibraryRepository, IDisposable
{
    private static readonly Action<ILogger, string, Exception?> DatabasePathLog = LoggerMessage.Define<string>(
        LogLevel.Information,
        new EventId(2001, nameof(DatabasePathLog)),
        "Using library catalog at {DatabasePath}");

    private readonly string _databasePath;
    private readonly ILogger<SqliteLibraryRepository> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public SqliteLibraryRepository(IOptions<LibraryOptions> options, ILogger<SqliteLibraryRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _databasePath = options.Value.DatabasePath;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var fullPath = GetDatabasePath();
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            DatabasePathLog(_logger, fullPath, null);

            using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE IF NOT EXISTS Songs (" +
                "Id TEXT PRIMARY KEY, " +
                "RootName TEXT NOT NULL, " +
                "RelativePath TEXT NOT NULL, " +
                "Title TEXT NOT NULL, " +
                "Artist TEXT NOT NULL, " +
                "ChannelConfiguration TEXT NOT NULL, " +
                "Priority INTEGER NOT NULL, " +
                "UpdatedAt TEXT NOT NULL" +
                ");";

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IReadOnlyList<SongRecord>> GetSongsAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT Id, RootName, RelativePath, Title, Artist, ChannelConfiguration, Priority, UpdatedAt " +
            "FROM Songs";

        var songs = new List<SongRecord>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            songs.Add(MapSong(reader));
        }

        return songs;
    }

    public async Task UpsertSongAsync(SongRecord song, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(song);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO Songs (Id, RootName, RelativePath, Title, Artist, ChannelConfiguration, Priority, UpdatedAt) " +
            "VALUES (@Id, @RootName, @RelativePath, @Title, @Artist, @ChannelConfiguration, @Priority, @UpdatedAt) " +
            "ON CONFLICT(Id) DO UPDATE SET " +
            "RootName = excluded.RootName, " +
            "RelativePath = excluded.RelativePath, " +
            "Title = excluded.Title, " +
            "Artist = excluded.Artist, " +
            "ChannelConfiguration = excluded.ChannelConfiguration, " +
            "Priority = excluded.Priority, " +
            "UpdatedAt = excluded.UpdatedAt;";

        AddSongParameters(command, song);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAllSongsAsync(CancellationToken cancellationToken)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Songs";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private SqliteConnection CreateConnection()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(),
            ForeignKeys = true,
        };

        return new SqliteConnection(connectionString.ToString());
    }

    private string GetDatabasePath()
    {
        if (Path.IsPathFullyQualified(_databasePath))
        {
            return _databasePath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _databasePath));
    }

    private static SongRecord MapSong(SqliteDataReader record)
    {
        return new SongRecord(
            record.GetString(0),
            record.GetString(1),
            record.GetString(2),
            record.GetString(3),
            record.GetString(4),
            record.GetString(5),
            record.GetInt32(6),
            DateTimeOffset.Parse(record.GetString(7), CultureInfo.InvariantCulture));
    }

    private static void AddSongParameters(SqliteCommand command, SongRecord song)
    {
        command.Parameters.AddWithValue("@Id", song.Id);
        command.Parameters.AddWithValue("@RootName", song.RootName);
        command.Parameters.AddWithValue("@RelativePath", song.RelativePath);
        command.Parameters.AddWithValue("@Title", song.Title);
        command.Parameters.AddWithValue("@Artist", song.Artist);
        command.Parameters.AddWithValue("@ChannelConfiguration", song.ChannelConfiguration);
        command.Parameters.AddWithValue("@Priority", song.Priority);
        command.Parameters.AddWithValue("@UpdatedAt", song.UpdatedAt.ToString("O"));
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
