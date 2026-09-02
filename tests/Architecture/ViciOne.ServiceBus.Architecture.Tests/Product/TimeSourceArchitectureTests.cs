using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class TimeSourceArchitectureTests
{
    private static readonly Regex DirectProcessClock = new(
        @"\b(?:DateTime|DateTimeOffset)\.(?:Now|UtcNow|Today)\b|\bStopwatch\.StartNew\s*\(",
        RegexOptions.CultureInvariant);

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
