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
                "UpdatedAt TEXT NOT NULL, " +
                "Language TEXT, " +
                "Genre TEXT, " +
                "Comment TEXT, " +
                "Instrumental INTEGER NOT NULL DEFAULT 0" +
                ");";

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            // Add new columns if they don't exist (for existing databases)
            await AddColumnIfNotExistsAsync(connection, "Songs", "Language", "TEXT", cancellationToken).ConfigureAwait(false);
            await AddColumnIfNotExistsAsync(connection, "Songs", "Genre", "TEXT", cancellationToken).ConfigureAwait(false);
            await AddColumnIfNotExistsAsync(connection, "Songs", "Comment", "TEXT", cancellationToken).ConfigureAwait(false);
            await AddColumnIfNotExistsAsync(connection, "Songs", "Instrumental", "INTEGER NOT NULL DEFAULT 0", cancellationToken).ConfigureAwait(false);
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
            "SELECT Id, RootName, RelativePath, Title, Artist, ChannelConfiguration, Priority, UpdatedAt, Language, Genre, Comment, Instrumental " +
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
            "INSERT INTO Songs (Id, RootName, RelativePath, Title, Artist, ChannelConfiguration, Priority, UpdatedAt, Language, Genre, Comment, Instrumental) " +
            "VALUES (@Id, @RootName, @RelativePath, @Title, @Artist, @ChannelConfiguration, @Priority, @UpdatedAt, @Language, @Genre, @Comment, @Instrumental) " +
            "ON CONFLICT(Id) DO UPDATE SET " +
            "RootName = excluded.RootName, " +
            "RelativePath = excluded.RelativePath, " +
            "Title = excluded.Title, " +
            "Artist = excluded.Artist, " +
            "ChannelConfiguration = excluded.ChannelConfiguration, " +
            "Priority = excluded.Priority, " +
            "UpdatedAt = excluded.UpdatedAt, " +
            "Language = excluded.Language, " +
            "Genre = excluded.Genre, " +
            "Comment = excluded.Comment, " +
            "Instrumental = excluded.Instrumental;";

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

    public async Task DeleteSongsByRootAsync(string rootName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rootName);

        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Songs WHERE RootName = @RootName";
        command.Parameters.AddWithValue("@RootName", rootName);
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
            DateTimeOffset.Parse(record.GetString(7), CultureInfo.InvariantCulture),
            record.IsDBNull(8) ? null : record.GetString(8),
            record.IsDBNull(9) ? null : record.GetString(9),
            record.IsDBNull(10) ? null : record.GetString(10),
            record.IsDBNull(11) ? 0 : record.GetInt32(11));
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
        command.Parameters.AddWithValue("@Language", (object?)song.Language ?? DBNull.Value);
        command.Parameters.AddWithValue("@Genre", (object?)song.Genre ?? DBNull.Value);
        command.Parameters.AddWithValue("@Comment", (object?)song.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("@Instrumental", song.Instrumental);
    }

    private static async Task AddColumnIfNotExistsAsync(SqliteConnection connection, string tableName, string columnName, string columnType, CancellationToken cancellationToken)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName})";

        var columnExists = false;
        using var reader = await checkCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var existingColumnName = reader.GetString(1);
            if (existingColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase))
            {
                columnExists = true;
                break;
            }
        }

        if (!columnExists)
        {
            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";
            await alterCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _semaphore.Dispose();
    }
}
