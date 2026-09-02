namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

using System.Security.Cryptography;
using System.Text;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class TestFrameworkCapabilityDispositionTests
{
    private const string ExpectedLegacyPathSetSha256 =
        "4bdcaf2ecee850fe4fbd7abae0891baa6397528581d5972a30100addd5aed1a7";

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "test-framework-capabilities-are-terminal-before-retirement")]
    public void TestFrameworkCapabilities_ArePathCompleteTerminalAndRetiredFromTheBuildGraph()
    {
        string mapPath = Path.Combine(
            RepositoryLayout.Root,
            ".testagent",
            "testframework-capability-disposition.tsv");
        string[] lines = File.ReadAllLines(mapPath);
        Assert.Equal("legacyPath\tdisposition\tnativeOwner\tclosureEvidence", lines[0]);

        MapRow[] rows = lines.Skip(1).Select(Parse).ToArray();
        string[] legacyPaths = rows.Select(row => row.LegacyPath)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string pathSetHash = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(string.Join('\n', legacyPaths) + "\n")))
            .ToLowerInvariant();

        Assert.Equal(147, rows.Length);
        Assert.Equal(147, legacyPaths.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedLegacyPathSetSha256, pathSetHash);
        Assert.Equal(9, rows.Count(row => row.Disposition == "REPLACED_EXECUTING"));
        Assert.Equal(135, rows.Count(row => row.Disposition == "RETIRED_SUPPORT_ONLY"));
        Assert.Equal(3, rows.Count(row => row.Disposition == "RETIRED_BUILD_INFRASTRUCTURE"));
        Assert.All(rows, row =>
        {
            Assert.Contains(row.Disposition, new[]
            {
                "REPLACED_EXECUTING",
                "RETIRED_SUPPORT_ONLY",
                "RETIRED_BUILD_INFRASTRUCTURE",
            });
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, row.NativeOwner)), row.NativeOwner);
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, row.ClosureEvidence)), row.ClosureEvidence);
        });

        Assert.False(Directory.Exists(RepositoryLayout.RetiredTestFrameworkDirectory));
        Assert.DoesNotContain(
            "ViciOne.ServiceBus.TestFramework",
            File.ReadAllText(Path.Combine(RepositoryLayout.Root, "ViciOne.ServiceBus.slnx")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "PackageVersion Include=\"NUnit",
            File.ReadAllText(Path.Combine(RepositoryLayout.Root, "Directory.Packages.props")),
            StringComparison.Ordinal);
    }

    private static MapRow Parse(string line)
    {
        string[] columns = line.Split('\t');
        Assert.Equal(4, columns.Length);
        Assert.All(columns, value => Assert.False(string.IsNullOrWhiteSpace(value)));
        return new MapRow(columns[0], columns[1], columns[2], columns[3]);
    }

    private sealed record MapRow(
        string LegacyPath,
        string Disposition,
        string NativeOwner,
        string ClosureEvidence);
}
