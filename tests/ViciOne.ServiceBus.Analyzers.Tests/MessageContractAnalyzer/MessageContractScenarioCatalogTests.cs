using ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class MessageContractScenarioCatalogTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "canonical-scenario-catalog-integrity")]
    public void Catalog_ContainsEveryCanonicalScenarioAndSourceFormExactlyOnce()
    {
        var scenarios = MessageContractScenarioCatalog.All;
        var cases = scenarios
            .SelectMany(scenario => scenario.EnumerateForms(), (scenario, form) => $"{scenario.Key}:{form}")
            .ToArray();

        Assert.Equal(48, scenarios.Length);
        Assert.Equal(scenarios.Length, scenarios.Select(scenario => scenario.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(91, cases.Length);
        Assert.Equal(cases.Length, cases.Distinct(StringComparer.Ordinal).Count());

        foreach (var scenario in scenarios)
        {
            foreach (var form in scenario.EnumerateForms())
            {
                var source = scenario.CreateSource(form);
                Assert.Equal(
                    1,
                    source.Split(MessageContractSourceFactory.TargetMarker, StringSplitOptions.None).Length - 1);
            }

            Assert.True(
                scenario.ExpectedAddedInitializers.Length == 0 || scenario.ExpectedDiagnostic?.Id == "MCA0003",
                $"Scenario '{scenario.Key}' requests a code fix without a missing-property diagnostic.");
        }
    }
}
