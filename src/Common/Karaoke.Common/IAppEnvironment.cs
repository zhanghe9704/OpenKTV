namespace Karaoke.Common;

public interface IAppEnvironment
{
    string EnvironmentName { get; }

    string ApplicationRootPath { get; }

    string ConfigurationRootPath { get; }

    string AssetsRootPath { get; }
}
