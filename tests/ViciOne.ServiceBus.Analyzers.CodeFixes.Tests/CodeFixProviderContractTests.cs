using Microsoft.CodeAnalysis.CodeFixes;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.CodeFixes.Tests;

public sealed class CodeFixProviderContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CODEFIX-PROVIDER-CONTRACT", "diagnostic-and-fix-all-metadata")]
    public void EveryProvider_DeclaresItsExactDiagnosticAndBatchFixAllProvider()
    {
        (CodeFixProvider Provider, string DiagnosticId)[] providers =
        [
            (new global::ViciOne.ServiceBus.Analyzers.CodeFixes.CancellationTokenOverloadMethodFixer(), "VOSB2001"),
            (new global::ViciOne.ServiceBus.Analyzers.CodeFixes.MessageContractCodeFixProvider(), "VOSB1004"),
        ];

        foreach (var (provider, diagnosticId) in providers)
        {
            Assert.Equal([diagnosticId], provider.FixableDiagnosticIds);
            Assert.Same(WellKnownFixAllProviders.BatchFixer, provider.GetFixAllProvider());
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CODEFIX-PROVIDER-CONTRACT", "exported-provider-surface")]
    public void CodeFixAssembly_ExportsOnlyTheTwoCodeFixProviders()
    {
        var exportedTypes = typeof(global::ViciOne.ServiceBus.Analyzers.CodeFixes.MessageContractCodeFixProvider).Assembly
            .GetExportedTypes()
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                nameof(global::ViciOne.ServiceBus.Analyzers.CodeFixes.CancellationTokenOverloadMethodFixer),
                nameof(global::ViciOne.ServiceBus.Analyzers.CodeFixes.MessageContractCodeFixProvider),
            ],
            exportedTypes);
    }
}
