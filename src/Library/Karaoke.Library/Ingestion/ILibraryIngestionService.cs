namespace Karaoke.Library.Ingestion;

public interface ILibraryIngestionService
{
    Task<LibraryIngestionResult> ScanAsync(CancellationToken cancellationToken);
    Task<LibraryIngestionResult> ScanSpecificRootsAsync(IEnumerable<string> rootNames, CancellationToken cancellationToken);
}

public sealed record LibraryIngestionResult(int FilesProcessed, int FilesSkipped);
