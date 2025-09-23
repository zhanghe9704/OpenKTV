using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Karaoke.Common;
using Karaoke.Library;
using Karaoke.Player;
using Karaoke.UI.ViewModels;
using Karaoke.UI.ViewModels.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Karaoke.UI;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        base.OnLaunched(args);

        _host = CreateHostBuilder(args.Arguments).Build();
        _host.Start();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Activate();
        window.Closed += async (_, _) => await StopHostAsync().ConfigureAwait(false);
    }

    private static IHostBuilder CreateHostBuilder(string? launchArguments)
    {
        launchArguments ??= string.Empty;

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(context.HostingEnvironment.ContentRootPath);
                config.AddJsonFile("config/settings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"config/settings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                config.AddEnvironmentVariables(prefix: "KARAOKE_");
                if (!string.IsNullOrWhiteSpace(launchArguments))
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["App:LaunchArguments"] = launchArguments,
                    });
                }
            })
            .ConfigureServices((context, services) =>
            {
                services.AddKaraokeCommonServices(context.Configuration);
                services.AddKaraokeLibrary();
                services.AddKaraokePlayer();

                services.AddSingleton<LibrarySettingsViewModel>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();
            });
    }

    private async Task StopHostAsync()
    {
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync().ConfigureAwait(false);
        _host.Dispose();
        _host = null;
    }
}
