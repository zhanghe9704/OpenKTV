namespace Karaoke.Library.Ingestion;

public interface ILibraryIngestionService
{
    Task<LibraryIngestionResult> ScanAsync(CancellationToken cancellationToken, IProgress<ScanProgress>? progress = null);
    Task<LibraryIngestionResult> ScanSpecificRootsAsync(IEnumerable<string> rootNames, CancellationToken cancellationToken, IProgress<ScanProgress>? progress = null);
}

public sealed record LibraryIngestionResult(int FilesProcessed, int FilesSkipped);

public sealed record ScanProgress(string RootName, int FilesScanned, string? CurrentFile = null);
