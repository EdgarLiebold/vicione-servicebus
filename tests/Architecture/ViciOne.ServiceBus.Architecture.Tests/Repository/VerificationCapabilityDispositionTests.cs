using System.Security.Cryptography;
using System.Text;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;
/// <summary>Terminal boundary between native MTP tests and retained engineering utilities.</summary>
public sealed class VerificationCapabilityDispositionTests
{
    private const string ExpectedLegacyPathSetSha256 =
        "caa89cb0b4ab159643455309065d1fa6776cc38cae3e80b1ca768f3ebc87374f";

    private static readonly IReadOnlyDictionary<string, int> ExpectedDispositions =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["RELOCATED_ENGINEERING"] = 2,
            ["REPLACED_NATIVE_TEST"] = 11,
            ["RETAINED_ENGINEERING"] = 10,
            ["RETAINED_NATIVE_CI"] = 1,
            ["RETIRED_HISTORICAL_REPRODUCTION"] = 1,
            ["RETIRED_LEGACY_VERDICT"] = 21,
            ["RETIRED_ONE_TIME_MIGRATION"] = 10,
            ["RETIRED_SECOND_WORKFLOW"] = 1,
        };

    private static readonly string[] ExpectedPythonFiles =
    [
        "tools/ci/aggregate_coverage_receipts.py",
        "tools/ci/coverage_receipt.py",
        "tools/ci/fixtures/__init__.py",
        "tools/ci/fixtures/broker_logs.py",
        "tools/ci/fixtures/compose_fixture.py",
        "tools/ci/fixtures/outage_protocol.py",
        "tools/ci/fixtures/run_scope.py",
        "tools/ci/run_broker_category.py",
        "tools/ci/vulnerability_inventory.py",
        "tools/identity/artifact_gate.py",
        "tools/identity/identity_gate.py",
        "tools/identity/identity_rules.py",
    ];

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "legacy-verification-capabilities-are-terminal-and-one-native-verdict-remains")]
    public void LegacyVerificationCapabilities_ArePathCompleteTerminalAndLeaveOneNativeVerdict()
    {
        string mapPath = Path.Combine(
            RepositoryLayout.Root,
            "evidence",
            "native-tests",
            "obligation-maps",
            "verification-capability-disposition.tsv");
        string[] lines = File.ReadAllLines(mapPath);
        Assert.Equal("legacyPath\tdisposition\tfinalOwner\tclosureEvidence", lines[0]);

        MapRow[] rows = lines.Skip(1).Select(Parse).ToArray();
        string[] legacyPaths = rows.Select(row => row.LegacyPath)
            .Order(StringComparer.Ordinal)
            .ToArray();
        string pathSetHash = Convert.ToHexString(SHA256.HashData(
                Encoding.UTF8.GetBytes(string.Join('\n', legacyPaths) + "\n")))
            .ToLowerInvariant();

        Assert.Equal(57, rows.Length);
        Assert.Equal(57, legacyPaths.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedLegacyPathSetSha256, pathSetHash);
        Dictionary<string, int> actualDispositions = rows
            .GroupBy(row => row.Disposition, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        Assert.Equal(ExpectedDispositions.Count, actualDispositions.Count);
        Assert.All(ExpectedDispositions, pair =>
            Assert.Equal(pair.Value, actualDispositions[pair.Key]));

        Assert.All(rows, row =>
        {
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, row.FinalOwner)), row.FinalOwner);
            Assert.True(File.Exists(Path.Combine(RepositoryLayout.Root, row.ClosureEvidence)), row.ClosureEvidence);

            bool retainedInPlace = row.Disposition is "RETAINED_ENGINEERING" or "RETAINED_NATIVE_CI";
            Assert.Equal(
                retainedInPlace,
                File.Exists(Path.Combine(RepositoryLayout.Root, row.LegacyPath)));
        });

        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "build", "verification")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "tools", "ci", "tests")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "tools", "ci", "verification")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryLayout.Root, "tests2")));

        string[] workflows = Directory.GetFiles(
                Path.Combine(RepositoryLayout.Root, ".github", "workflows"),
                "*.yml",
                SearchOption.TopDirectoryOnly)
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal([".github/workflows/native-tests.yml"], workflows);

        string workflow = File.ReadAllText(Path.Combine(RepositoryLayout.Root, workflows[0]));
        Assert.Contains("dotnet test --solution ViciOne.ServiceBus.Tests.Unit.slnx", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet test --solution ViciOne.ServiceBus.Tests.LocalIntegration.slnx", workflow, StringComparison.Ordinal);
        Assert.Contains("dotnet pack ViciOne.ServiceBus.slnx", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/identity/identity_gate.py current", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/identity/artifact_gate.py", workflow, StringComparison.Ordinal);
        Assert.Contains("license/repository_diff.py", workflow, StringComparison.Ordinal);
        Assert.Contains("tools/ci/vulnerability_inventory.py", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("VERIFICATION_MODEL", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("tools/ci/verify.py", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("unittest", workflow, StringComparison.OrdinalIgnoreCase);

        string[] actualPythonFiles = new[] { "tools/ci", "tools/identity" }
            .SelectMany(directory => Directory.GetFiles(
                Path.Combine(RepositoryLayout.Root, directory),
                "*.py",
                SearchOption.AllDirectories))
            .Where(path => !path.Contains("__pycache__", StringComparison.Ordinal))
            .Select(RepositoryLayout.RelativeToRoot)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedPythonFiles, actualPythonFiles);
        Assert.All(actualPythonFiles, path =>
        {
            string source = File.ReadAllText(Path.Combine(RepositoryLayout.Root, path));
            Assert.DoesNotContain("import unittest", source, StringComparison.Ordinal);
            Assert.DoesNotContain("from unittest", source, StringComparison.Ordinal);
        });

        string[] buildPolicyFiles =
        [
            Path.Combine(RepositoryLayout.Root, "Directory.Packages.props"),
            .. RepositoryLayout.GovernedProjects,
        ];
        Assert.All(buildPolicyFiles, path =>
        {
            string content = File.ReadAllText(path);
            Assert.DoesNotContain("PackageReference Include=\"NUnit", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PackageReference Include=\"Microsoft.NET.Test.Sdk", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PackageReference Include=\"GitHubActionsTestLogger", content, StringComparison.OrdinalIgnoreCase);
        });

        string[] publicOwners =
        [
            Path.Combine(RepositoryLayout.Root, "README.md"),
            Path.Combine(RepositoryLayout.Root, "CONTRIBUTING.md"),
            Path.Combine(RepositoryLayout.Root, "TODO.md"),
            Path.Combine(RepositoryLayout.Root, "docs", "build.md"),
            Path.Combine(RepositoryLayout.Root, "Directory.Build.props"),
            Path.Combine(RepositoryLayout.Root, "Directory.Build.targets"),
            .. Directory.GetFiles(RepositoryLayout.Root, "*.slnx", SearchOption.TopDirectoryOnly),
        ];
        Assert.All(publicOwners, path =>
            Assert.DoesNotContain("tests2", File.ReadAllText(path), StringComparison.Ordinal));
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
        string FinalOwner,
        string ClosureEvidence);
}
