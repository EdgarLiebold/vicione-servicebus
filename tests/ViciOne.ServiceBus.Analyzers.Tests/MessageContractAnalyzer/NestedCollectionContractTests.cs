using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class NestedCollectionContractTests
{
    [Theory]
    [InlineData("IReadOnlyList<IReadOnlyList<Item>>", "new[] { new[] { new { Id = 17 } } }", "VOSB1004", "Items.Price")]
    [InlineData("Item[][]", "new[] { new[] { new { Id = 17 } } }", "VOSB1004", "Items.Price")]
    [InlineData("Item[][][]", "new[] { new[] { new[] { new { Id = 17 } } } }", "VOSB1004", "Items.Price")]
    [InlineData("IReadOnlyList<IReadOnlyList<Item>>", "new[] { new[] { new { Id = 17, Price = Guid.Empty } } }", "VOSB1002", "Items.Price")]
    [InlineData("Item[][]", "new[] { new[] { new { Id = 17, Price = Guid.Empty } } }", "VOSB1002", "Items.Price")]
    [InlineData("Item[][][]", "new[] { new[] { new[] { new { Id = 17, Price = Guid.Empty } } } }", "VOSB1002", "Items.Price")]
    [InlineData("IReadOnlyList<IReadOnlyList<Item>>", "new[] { new[] { new { Id = 17, Price = 2.5m } } }", null, null)]
    [InlineData("Item[][]", "new[] { new[] { new { Id = 17, Price = 2.5m } } }", null, null)]
    [InlineData("Item[][][]", "new[] { new[] { new[] { new { Id = 17, Price = 2.5m } } } }", null, null)]
    [InlineData("IReadOnlyList<IReadOnlyList<int>>", "new[] { 17, 29 }", null, null)]
    [InlineData("int[][]", "new[] { 17, 29 }", null, null)]
    [InlineData("int[][]", "new[] { Guid.Empty }", "VOSB1002", "Items")]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "nested-collection-leaf-compatibility-and-missing-properties")]
    public async Task NestedCollections_ReportLeafContractsWithoutCollectionImplementationPropertiesAsync(
        string collectionType, string expression, string? diagnosticId, string? propertyPath)
    {
        string source = ServiceBusAnalyzerFixture.Usings + $$"""
            namespace NestedContracts
            {
                public interface Message { {{collectionType}} Items { get; } }
                public interface Item { int Id { get; } decimal Price { get; } }
                class Program
                {
                    static async Task Main()
                    {
                        var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
                        await bus.PublishAsync<Message>(new { Items = {{expression}} });
                    }
                }
            }
            """;
        var diagnostics = await RoslynTestHost.AnalyzeAsync(source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots, TestContext.Current.CancellationToken);

        if (diagnosticId is null)
        {
            Assert.Empty(diagnostics);
            return;
        }

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(diagnosticId, diagnostic.Id);
        Assert.Equal(diagnosticId == "VOSB1004" ? DiagnosticSeverity.Info : DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(diagnosticId == "VOSB1004"
            ? $"Message values for contract 'Message' are missing properties: {propertyPath}"
            : $"Message values do not map to contract 'Message'; incompatible properties: {propertyPath}", diagnostic.Message);
    }
}
