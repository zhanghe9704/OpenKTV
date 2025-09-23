using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Karaoke.Common;

public sealed class AppEnvironment : IAppEnvironment
{
    private readonly AppEnvironmentOptions _options;
    private readonly IHostEnvironment _hostEnvironment;

    public AppEnvironment(IOptions<AppEnvironmentOptions> options, IHostEnvironment hostEnvironment)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    }

    public string EnvironmentName => string.IsNullOrWhiteSpace(_options.EnvironmentName)
        ? _hostEnvironment.EnvironmentName
        : _options.EnvironmentName;

    public string ApplicationRootPath => _hostEnvironment.ContentRootPath;

    public string ConfigurationRootPath => ResolvePath(_options.ConfigurationRoot);

    public string AssetsRootPath => ResolvePath(_options.AssetsRoot);

    private string ResolvePath(string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
        {
            return _hostEnvironment.ContentRootPath;
        }

        return Path.IsPathFullyQualified(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, relativeOrAbsolute));
    }
}
