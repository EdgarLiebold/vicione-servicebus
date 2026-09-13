using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Analyzers.MessageContracts;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class MessageContractScenarioTests
{
    public static TheoryData<string, MessageSourceForm> Cases()
    {
        var cases = new TheoryData<string, MessageSourceForm>();
        foreach (var scenario in MessageContractScenarioCatalog.All)
        {
            foreach (var form in scenario.EnumerateForms())
                cases.Add(scenario.Key, form);
        }

        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "canonical-scenario-catalog")]
    public async Task MessageSource_ProducesItsExactContractDiagnosticAsync(
        string scenarioKey,
        MessageSourceForm form)
    {
        var scenario = MessageContractScenarioCatalog.Get(scenarioKey);
        var source = scenario.CreateSource(form);

        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        if (scenario.ExpectedDiagnostic is null)
        {
            Assert.Empty(actual);
            return;
        }

        var diagnostic = scenario.ExpectedDiagnostic;
        var (line, column) = TargetAnonymousObjectLocation(source);
        var expected = new DiagnosticObservation(
            diagnostic.Id,
            diagnostic.Severity,
            diagnostic.Message,
            "Test0.cs",
            line,
            column);

        Assert.Equal([expected], actual);
    }

    private static (int Line, int Column) TargetAnonymousObjectLocation(string source)
    {
        var markerIndex = source.IndexOf(MessageContractSourceFactory.TargetMarker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "The source must contain the canonical target marker.");

        var targetIndex = source.IndexOf("new", markerIndex + MessageContractSourceFactory.TargetMarker.Length, StringComparison.Ordinal);
        Assert.True(targetIndex >= 0, "The marked source must be followed by an anonymous-object creation.");

        var prefix = source.AsSpan(0, targetIndex);
        var line = 1;
        var column = 1;

        foreach (var character in prefix)
        {
            if (character == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}
