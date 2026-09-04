using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

public sealed class BenchmarkObligationProjectionTests
{
    private const string ExpectedLegacyIdentitySetSha256 =
        "5f9bf96bc6cd9687fd116068849efe624e7ebd8e91a498b25058a1318ee66cf2";

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "benchmark-obligations-are-terminal-and-carrier-bound")]
    public void BenchmarkObligations_AreExactTerminalAndBoundToNativeCarriers()
    {
        string mapPath = Path.Combine(
            RepositoryLayout.Root,
            "evidence",
            "native-tests",
            "obligation-maps",
            "benchmark-native-obligation-map.tsv");
        string[] lines = File.ReadAllLines(mapPath);
        Assert.Equal("legacyIdentity\tdisposition\tprofile\ttargetProject\ttargetMethod", lines[0]);
        MapRow[] rows = lines.Skip(1).Select(Parse).ToArray();
        string[] legacyIdentities = rows.Select(row => row.LegacyIdentity)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string legacyIdentityHash = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(string.Join('\n', legacyIdentities) + "\n")))
            .ToLowerInvariant();

        Assert.Equal(75, rows.Length);
        Assert.Equal(ExpectedLegacyIdentitySetSha256, legacyIdentityHash);
        Assert.Equal(75, rows.Select(row => row.LegacyIdentity).Distinct(StringComparer.Ordinal).Count());
        Assert.All(rows, row =>
        {
            Assert.Equal("REPLACED_EXECUTING", row.Disposition);
            Assert.Equal("UnitArchitecture", row.Profile);
            Assert.Equal(
                "tests/Benchmarks/ViciOne.ServiceBus.Benchmark.Tests/ViciOne.ServiceBus.Benchmark.Tests.csproj",
                row.TargetProject);
        });

        string[] carriers = ReadCompiledRequirementCarriers();
        Assert.All(rows, row => Assert.Contains(row.TargetMethod, carriers));
        Assert.False(Directory.Exists(Path.Combine(
            RepositoryLayout.Root,
            "benchmarks",
            "ViciOne.ServiceBus.Benchmarks.Tests")));
    }

    private static MapRow Parse(string line)
    {
        string[] columns = line.Split('\t');
        Assert.Equal(5, columns.Length);
        Assert.All(columns, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        return new MapRow(columns[0], columns[1], columns[2], columns[3], columns[4]);
    }

    private static string[] ReadCompiledRequirementCarriers()
    {
        string path = Path.Combine(
            RepositoryLayout.Root,
            "tests",
            "Benchmarks",
            "ViciOne.ServiceBus.Benchmark.Tests",
            "Requirements",
            "BenchmarkRequirements.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));

        return document.RootElement.EnumerateArray()
            .Select(row => $"{row.GetProperty("testType").GetString()}.{row.GetProperty("testMethod").GetString()}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record MapRow(
        string LegacyIdentity,
        string Disposition,
        string Profile,
        string TargetProject,
        string TargetMethod);
}
