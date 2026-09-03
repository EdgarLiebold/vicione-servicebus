using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class TimeSourceArchitectureTests
{
    private static readonly Regex DirectWallClock = new(
        @"\b(?:DateTime|DateTimeOffset)\.(?:Now|UtcNow|Today)\b",
        RegexOptions.CultureInvariant);

    private static readonly Regex DirectProcessClock = new(
        @"\b(?:DateTime|DateTimeOffset)\.(?:Now|UtcNow|Today)\b|\bStopwatch\.StartNew\s*\(",
        RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<string, int> ReviewedWallClockAllowlist =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["src/Transports/ViciOne.ServiceBus.RabbitMqTransport/RabbitMqTransport/RabbitMqAddressExtensions.cs"] = 1,
            ["src/ViciOne.ServiceBus.Abstractions/NewId/NewIdProviders/DateTimeTickProvider.cs"] = 1,
            ["src/ViciOne.ServiceBus.Abstractions/NewId/NewIdProviders/StopwatchTickProvider.cs"] = 1,
            ["src/ViciOne.ServiceBus/Agents/AsyncPipeContextHandle.cs"] = 1,
            ["src/ViciOne.ServiceBus/Agents/PipeContextAgent.cs"] = 1,
            ["src/ViciOne.ServiceBus/Events/FaultEvent.cs"] = 1,
            ["src/ViciOne.ServiceBus/Events/ReceiveFaultEvent.cs"] = 1,
            ["src/ViciOne.ServiceBus/Introspection/ProbeResultBuilder.cs"] = 2,
            ["src/ViciOne.ServiceBus/Logging/Diagnostics/StartedActivity.cs"] = 1,
            ["src/ViciOne.ServiceBus/Logging/TextWriterLogger.cs"] = 1,
            ["src/ViciOne.ServiceBus/Middleware/Rescue/RescueExceptionReceiveContext.cs"] = 1,
        };

    [Fact]
    [RequirementCoverage("REQ-VSB-TIME-SOURCE-ARCHITECTURE", "production-runtime-direct-clock-allowlist")]
    public void ProductionRuntimePaths_KeepDirectWallClockUsesWithinReviewedAllowlist()
    {
        string sourceRoot = Path.Combine(RepositoryLayout.Root, "src");

        Dictionary<string, int> actual = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(filePath => File.ReadLines(filePath)
                .Where(line => DirectWallClock.IsMatch(line))
                .Select(_ => RepositoryLayout.RelativeToRoot(filePath)))
            .GroupBy(relativePath => relativePath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        string[] unexpected = actual.Keys
            .Except(ReviewedWallClockAllowlist.Keys, StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unexpected);
        Assert.Equal(ReviewedWallClockAllowlist.Count, actual.Count);

        foreach ((string relativePath, int expectedCount) in ReviewedWallClockAllowlist)
        {
            Assert.True(actual.TryGetValue(relativePath, out int actualCount), $"Missing reviewed wall-clock use: {relativePath}");
            Assert.Equal(expectedCount, actualCount);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TIME-SOURCE-ARCHITECTURE", "future-and-job-metadata-use-context-bound-time")]
    public void FutureAndJobMetadataPaths_UseOnlyContextBoundTimeSources()
    {
        string[] roots =
        [
            Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "Futures"),
            Path.Combine(RepositoryLayout.Root, "src", "ViciOne.ServiceBus", "JobService"),
        ];
        string schedulingPolicy = Path.GetFullPath(Path.Combine(
            RepositoryLayout.Root,
            "src",
            "ViciOne.ServiceBus",
            "JobService",
            "JobService",
            "Scheduling",
            "Defaults.cs"));

        string[] violations = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => !RepositoryLayout.PathComparer.Equals(Path.GetFullPath(path), schedulingPolicy))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => (line, index))
                .Where(candidate => DirectProcessClock.IsMatch(candidate.line))
                .Select(candidate =>
                    $"{RepositoryLayout.RelativeToRoot(path)}:{candidate.index + 1}: {candidate.line.Trim()}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }
}
