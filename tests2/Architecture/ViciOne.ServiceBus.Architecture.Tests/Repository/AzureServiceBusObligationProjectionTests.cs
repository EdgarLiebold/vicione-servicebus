namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AzureServiceBusObligationProjectionTests
{
    private const string ExpectedObligationSetSha256 =
        "7992dce36627b371732aae887c5f5693dbd9eb5b4274bf5d5f6c764c545621fc";

    private static readonly string[] ProjectionPaths =
    [
        ".testagent/azure-servicebus-hermetic-native-obligation-map.tsv",
        ".testagent/azure-servicebus-local-baseline-native-obligation-map.tsv",
        ".testagent/azure-servicebus-provider-neutral-native-obligation-map.tsv",
        ".testagent/azure-servicebus-external-native-obligation-map.tsv",
    ];

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "azure-service-bus-obligations-are-terminal-and-carrier-bound")]
    public void AzureServiceBusObligations_AreExactTerminalAndBoundToRealCarriers()
    {
        MapRow[] rows = ProjectionPaths.SelectMany(ReadProjection).ToArray();
        string[] ids = rows.Select(row => row.ObligationId).Order(StringComparer.Ordinal).ToArray();
        string identitySet = string.Join('\n', ids) + "\n";
        string identitySha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identitySet)))
            .ToLowerInvariant();

        Assert.Equal(153, rows.Length);
        Assert.Equal(153, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedObligationSetSha256, identitySha);
        Assert.Equal(107, rows.Count(row => row.Disposition == "REPLACED_EXECUTING"));
        Assert.Equal(46, rows.Count(row => row.Disposition == "EXTERNAL_PENDING"));
        Assert.Equal(58, rows.Count(row => row.Profile == "UnitArchitecture"));
        Assert.Equal(49, rows.Count(row => row.Profile == "AzureServiceBusLocalIntegration"));
        Assert.Equal(46, rows.Count(row => row.Profile == "External"));

        string[] compiledCarriers = ReadCompiledRequirementCarriers();
        foreach (MapRow row in rows)
        {
            if (row.Disposition == "REPLACED_EXECUTING")
            {
                Assert.StartsWith("tests2/", row.TargetProject, StringComparison.Ordinal);
                Assert.True(
                    compiledCarriers.Any(carrier => carrier.EndsWith(row.TargetMethod, StringComparison.Ordinal)),
                    $"Executing Azure Service Bus obligation {row.ObligationId} has no compiled requirement carrier: {row.TargetMethod}");
            }
            else
            {
                Assert.Equal("EXTERNAL_PENDING", row.Disposition);
                Assert.Equal("External", row.Profile);
                Assert.Equal(
                    "TODO.md#complete-azure-service-bus-validation-against-the-real-cloud-service",
                    row.TargetProject);
            }
        }

        Assert.False(Directory.Exists(Path.Combine(
            RepositoryLayout.Root,
            "tests",
            "Transports",
            "ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests")));
    }

    private static IEnumerable<MapRow> ReadProjection(string relativePath)
    {
        string path = Path.Combine(RepositoryLayout.Root, relativePath);
        string[] lines = File.ReadAllLines(path);

        Assert.NotEmpty(lines);
        Assert.Equal("obligationId\tdisposition\tprofile\ttargetProject\ttargetMethod", lines[0]);

        foreach (string line in lines.Skip(1))
        {
            string[] columns = line.Split('\t');
            Assert.Equal(5, columns.Length);
            Assert.All(columns, value => Assert.False(string.IsNullOrWhiteSpace(value)));
            yield return new MapRow(columns[0], columns[1], columns[2], columns[3], columns[4]);
        }
    }

    private static string[] ReadCompiledRequirementCarriers()
    {
        string testsRoot = Path.Combine(RepositoryLayout.Root, "tests2");
        var carriers = new List<string>();

        foreach (string path in Directory.EnumerateFiles(testsRoot, "*.json", SearchOption.AllDirectories)
                     .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Requirements{Path.DirectorySeparatorChar}",
                         StringComparison.Ordinal)))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                if (row.TryGetProperty("testType", out JsonElement testType)
                    && row.TryGetProperty("testMethod", out JsonElement testMethod))
                    carriers.Add($"{testType.GetString()}.{testMethod.GetString()}");
            }
        }

        return carriers.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }

    private sealed record MapRow(
        string ObligationId,
        string Disposition,
        string Profile,
        string TargetProject,
        string TargetMethod);
}
