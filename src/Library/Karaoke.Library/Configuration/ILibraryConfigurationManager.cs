namespace Karaoke.Library.Configuration;

public interface ILibraryConfigurationManager
{
    Task<IReadOnlyList<LibraryRootOptions>> GetRootsAsync(CancellationToken cancellationToken);

    Task SaveRootsAsync(IEnumerable<LibraryRootOptions> roots, CancellationToken cancellationToken);

    Task<LibraryOptions> GetLibraryOptionsAsync(CancellationToken cancellationToken);

    Task SaveLibraryOptionsAsync(LibraryOptions options, CancellationToken cancellationToken);
}
