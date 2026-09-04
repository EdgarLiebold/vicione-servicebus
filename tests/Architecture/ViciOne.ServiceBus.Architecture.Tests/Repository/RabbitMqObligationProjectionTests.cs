using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Repository;

public sealed class RabbitMqObligationProjectionTests
{
    private const string ExpectedObligationSetSha256 =
        "a8f551cd4df8b334a235308836582a4a7b0ed7c9a3d508630e547c9df4e2bb5b";

    private static readonly string[] ProjectionPaths =
    [
        "evidence/native-tests/obligation-maps/rabbitmq-unit-native-obligation-map.tsv",
        "evidence/native-tests/obligation-maps/rabbitmq-local-native-obligation-map.tsv",
        "evidence/native-tests/obligation-maps/rabbitmq-external-native-obligation-map.tsv",
    ];

    [Fact]
    [RequirementCoverage("REQ-TEST-205", "rabbitmq-obligations-are-terminal-and-carrier-bound")]
    public void RabbitMqObligations_AreExactTerminalAndBoundToRealCarriers()
    {
        MapRow[] rows = ProjectionPaths.SelectMany(ReadProjection).ToArray();
        string[] ids = rows.Select(row => row.ObligationId).Order(StringComparer.Ordinal).ToArray();
        string identitySet = string.Join('\n', ids) + "\n";
        string identitySha = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identitySet)))
            .ToLowerInvariant();

        Assert.Equal(354, rows.Length);
        Assert.Equal(354, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(ExpectedObligationSetSha256, identitySha);
        Assert.Equal(353, rows.Count(row => row.Disposition == "REPLACED_EXECUTING"));
        Assert.Single(rows, row => row.Disposition == "EXTERNAL_PENDING");
        Assert.Equal(191, rows.Count(row => row.Profile == "UnitArchitecture"));
        Assert.Equal(162, rows.Count(row => row.Profile == "RabbitMqLocalIntegration"));
        Assert.Single(rows, row => row.Profile == "External");

        string[] compiledCarriers = ReadCompiledRequirementCarriers();
        foreach (MapRow row in rows)
        {
            if (row.Disposition == "REPLACED_EXECUTING")
            {
                Assert.StartsWith("tests/", row.TargetProject, StringComparison.Ordinal);
                Assert.True(
                    compiledCarriers.Any(carrier => carrier.EndsWith(row.TargetMethod, StringComparison.Ordinal)),
                    $"Executing RabbitMQ obligation {row.ObligationId} has no compiled requirement carrier: {row.TargetMethod}");
            }
            else
            {
                Assert.Equal("EXTERNAL_PENDING", row.Disposition);
                Assert.Equal("OBL-R0-BRK-0004", row.ObligationId);
                Assert.Equal("External", row.Profile);
                Assert.Equal(
                    "TODO.md#complete-rabbitmq-validation-against-amazon-mq",
                    row.TargetProject);
            }
        }

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
        string testsRoot = Path.Combine(RepositoryLayout.Root, "tests");
        var carriers = new List<string>();

        foreach (string path in Directory.EnumerateFiles(testsRoot, "*.json", SearchOption.AllDirectories)
                     .Where(path => path.Contains(
                         $"{Path.DirectorySeparatorChar}Requirements{Path.DirectorySeparatorChar}",
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
