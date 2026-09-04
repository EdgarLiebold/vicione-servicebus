using System;
using System.IO;
using BenchmarkDotNet.Configs;

namespace ViciOne.ServiceBus.BenchmarkConsole;

public static class BenchmarkConfiguration
{
    const string ArtifactsEnvironmentVariable = "VICIONE_BENCHMARK_ARTIFACTS";

    public static IConfig Create()
    {
        return DefaultConfig.Instance
            .WithArtifactsPath(ResolveArtifactsPath());
    }

    public static string ResolveArtifactsPath()
    {
        var configuredPath = Environment.GetEnvironmentVariable(ArtifactsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        for (var directory = new DirectoryInfo(Environment.CurrentDirectory); directory != null; directory = directory.Parent)
        {
            // The canonical solution is the marker. The classic .sln it used to accept as well was
            // deleted with the migration, so that half of the condition could never match again and
            // only kept a second build truth alive.
            if (File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.slnx")))
                return Path.Combine(directory.FullName, "artifacts", "BenchmarkDotNet");
        }

        throw new InvalidOperationException(
            $"The repository root could not be found. Set {ArtifactsEnvironmentVariable} to an explicit artifacts directory.");
    }
}
