using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karaoke.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKaraokeCommonServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration ?? throw new ArgumentNullException(nameof(configuration));

        services.Configure<AppEnvironmentOptions>(configuration.GetSection(AppEnvironmentOptions.SectionName));
        services.AddSingleton<IAppEnvironment, AppEnvironment>();
        services.AddLogging(builder => builder
            .AddConfiguration(configuration.GetSection("Logging"))
            .AddDebug()
            .AddConsole());

        return services;
    }
}