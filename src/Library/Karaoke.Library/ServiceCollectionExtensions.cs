using Karaoke.Library.Configuration;
using Karaoke.Library.Ingestion;
using Karaoke.Library.Services;
using Karaoke.Library.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Karaoke.Library;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKaraokeLibrary(this IServiceCollection services)
    {
        services.AddOptions<LibraryOptions>()
            .BindConfiguration(LibraryOptions.SectionName);

        services.AddSingleton<ILibraryConfigurationManager, JsonLibraryConfigurationManager>();
        services.AddSingleton<ILibraryRepository, SqliteLibraryRepository>();
        services.AddSingleton<ILibraryService, LibraryService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMediaPathParser, DirectoryStructureParser>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMediaPathParser, HyphenFileNameParser>());
        services.AddSingleton<ILibraryIngestionService, LibraryIngestionService>();
        return services;
    }
}
