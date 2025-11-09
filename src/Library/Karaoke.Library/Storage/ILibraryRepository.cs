using Karaoke.Library.Storage.Models;

namespace Karaoke.Library.Storage;

public interface ILibraryRepository
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SongRecord>> GetSongsAsync(CancellationToken cancellationToken);

    Task UpsertSongAsync(SongRecord song, CancellationToken cancellationToken);

    Task UpdateNormalizationDataAsync(string songId, double? loudnessLufs, double? gainDb, CancellationToken cancellationToken);

    Task DeleteAllSongsAsync(CancellationToken cancellationToken);

    Task DeleteSongsByRootAsync(string rootName, CancellationToken cancellationToken);
}
