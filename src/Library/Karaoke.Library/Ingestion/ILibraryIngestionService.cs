namespace Karaoke.Library.Ingestion;

public interface ILibraryIngestionService
{
    Task<LibraryIngestionResult> ScanAsync(CancellationToken cancellationToken);
}

public sealed record LibraryIngestionResult(int FilesProcessed, int FilesSkipped);
