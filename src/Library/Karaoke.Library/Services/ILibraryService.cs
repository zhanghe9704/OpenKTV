using Karaoke.Common.Models;

namespace Karaoke.Library.Services;

public interface ILibraryService
{
    Task<IReadOnlyList<SongDto>> SearchAsync(string query, CancellationToken cancellationToken);

    Task<IReadOnlyList<SongDto>> GetAllSongsAsync(CancellationToken cancellationToken);

    Task UpsertAsync(SongDto song, CancellationToken cancellationToken);
}
