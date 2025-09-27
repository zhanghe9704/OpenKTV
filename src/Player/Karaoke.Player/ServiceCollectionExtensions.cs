using Karaoke.Player.Playback;
using Microsoft.Extensions.DependencyInjection;

namespace Karaoke.Player;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKaraokePlayer(this IServiceCollection services)
    {
        services.AddSingleton<IPlaybackService, VlcPlaybackService>();
        return services;
    }
}
