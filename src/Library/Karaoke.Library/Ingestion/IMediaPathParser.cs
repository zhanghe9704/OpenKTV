using Karaoke.Library.Configuration;

namespace Karaoke.Library.Ingestion;

public interface IMediaPathParser
{
    bool TryParse(MediaFileContext context, out ParsedSongMetadata metadata);
}

public readonly record struct MediaFileContext(
    string RootName,
    string RootPath,
    LibraryRootOptions RootOptions,
    LibraryOptions GlobalOptions,
    string FilePath);

public sealed record ParsedSongMetadata(
    string RelativePath,
    string Title,
    string Artist,
    string ChannelConfiguration,
    int Priority);
