using System.Text.RegularExpressions;
using ViciOne.ServiceBus.Architecture.Tests.Repository;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Architecture.Tests.Product;

public sealed class ProductDocumentationArchitectureTests
{
    private static readonly string[] ForbiddenProcessTerms =
    [
        @"\bcohorts?\b",
        @"\bslices?\b",
        @"\bred[ -]team\b",
        @"\bmutants?\b",
        @"\bmutations?\b",
        @"\bevidence/",
        @"\bWP-F2\b",
        @"\brecord-0",
        @"\bretir(?:e|ed|ement|ing)\b",
        @"\btakeover\b",
        @"\breviews?\b",
        @"\bdonors?\b",
        @"\bagents?\b",
        @"\bbaselines?\b",
        @"\binherited\b",
        @"\blegacy\b",
        @"\bupstream\b",
    ];

    [Fact]
    [RequirementCoverage("REQ-VSB-PRODUCT-DOCUMENTATION", "user-facing-documents-have-no-internal-process-narrative")]
    public void ProductDocumentation_DescribesTheCurrentProductWithoutInternalProcessNarrative()
    {
        string[] paths =
        [
            "README.md",
            "CONTRIBUTING.md",
            "docs/build.md",
            "docs/api-surface.md",
            "docs/observability.md",
            "docs/reliability.md",
            "docs/migrations/README.md",
        ];

        foreach (string path in paths)
        {
            string content = File.ReadAllText(Path.Combine(RepositoryLayout.Root, path));
            if (path == "README.md")
            {
                content = Regex.Replace(
                    content,
                    @"## Origin and license.*?(?=## Install and configure)",
                    string.Empty,
                    RegexOptions.Singleline | RegexOptions.CultureInvariant);
            }

            Assert.All(ForbiddenProcessTerms, pattern => Assert.DoesNotMatch(
                new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
                content));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABILITY-DOCUMENTATION", "data-flow-states-operations-and-api-moves-are-complete")]
    public void ProductDocumentation_CoversReliabilityStatesAndPublicApiChanges()
    {
        string reliability = File.ReadAllText(Path.Combine(RepositoryLayout.Root, "docs", "reliability.md"));
        Assert.Contains("flowchart LR", reliability, StringComparison.Ordinal);
        Assert.Contains("stateDiagram-v2", reliability, StringComparison.Ordinal);
        Assert.All(
            new[] { "Outbox", "Carrier", "Inbox", "Pending", "Processing", "RetryScheduled", "Quarantined", "Retried", "Abandoned", "TransportAccepted", "Applied" },
            value => Assert.Contains(value, reliability, StringComparison.Ordinal));

        string changelog = File.ReadAllText(Path.Combine(RepositoryLayout.Root, "CHANGELOG.md"));
        string migrationHeading = "Migration from " + string.Concat("Mass", "Transit") + "-style APIs";
        Assert.Contains(migrationHeading, changelog, StringComparison.Ordinal);
        Assert.All(
            new[]
            {
                "Changed call forms",
                "Moved namespaces and provider names",
                "New capability packages",
                "SendAsync",
                "PublishAsync",
                "GetResponseAsync",
                "UseReliableMessaging",
                "UseMessageJournal",
                "ViciOne.ServiceBus.Sagas",
                "ViciOne.ServiceBus.Courier",
                "ViciOne.ServiceBus.Futures",
                "ViciOne.ServiceBus.JobService",
                "ViciOne.ServiceBus.Mediator",
                "ViciOne.ServiceBus.Initializers",
                "ViciOne.ServiceBus.EntityFrameworkCore.Sagas",
            },
            value => Assert.Contains(value, changelog, StringComparison.Ordinal));
    }
}
