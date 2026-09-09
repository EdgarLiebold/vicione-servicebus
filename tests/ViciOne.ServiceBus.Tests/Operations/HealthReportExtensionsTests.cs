using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Operations;

public sealed class HealthReportExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HEALTH-REPORT-SERIALIZATION", "missing-report-boundary")]
    public void ToJsonString_RejectsAMissingReport()
    {
        Assert.Equal(
            "result",
            Assert.Throws<ArgumentNullException>(() => HealthReportExtensions.ToJsonString(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-HEALTH-REPORT-SERIALIZATION", "status-description-and-data-contract")]
    public void ToJsonString_WritesTheStableHealthReportContract()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["serviceBus"] = new(
                HealthStatus.Degraded,
                "delivery is delayed",
                TimeSpan.FromMilliseconds(25),
                exception: null,
                data: new Dictionary<string, object>
                {
                    ["pending"] = 3,
                    ["available"] = true,
                }),
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(30));

        string json = report.ToJsonString();
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement entry = root.GetProperty("results").GetProperty("serviceBus");

        Assert.Equal(HealthStatus.Degraded.ToString(), root.GetProperty("status").GetString());
        Assert.Equal(HealthStatus.Degraded.ToString(), entry.GetProperty("status").GetString());
        Assert.Equal("delivery is delayed", entry.GetProperty("description").GetString());
        Assert.Equal(3, entry.GetProperty("data").GetProperty("pending").GetInt32());
        Assert.True(entry.GetProperty("data").GetProperty("available").GetBoolean());
        Assert.DoesNotContain("duration", root.EnumerateObject().Select(property => property.Name));
        Assert.Contains(Environment.NewLine, json, StringComparison.Ordinal);
    }
}
