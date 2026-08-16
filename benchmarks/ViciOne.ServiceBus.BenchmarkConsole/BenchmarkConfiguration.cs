namespace ViciOne.ServiceBus.BenchmarkConsole;

using System;
using System.IO;
using BenchmarkDotNet.Configs;


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
            if (File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.sln"))
                || File.Exists(Path.Combine(directory.FullName, "ViciOne.ServiceBus.slnx")))
                return Path.Combine(directory.FullName, "artifacts", "BenchmarkDotNet");
        }

        throw new InvalidOperationException(
            $"The repository root could not be found. Set {ArtifactsEnvironmentVariable} to an explicit artifacts directory.");
    }
}
