using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class MessageContractRuleContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "supported-rules-are-executable")]
    public void SupportedDiagnostics_ContainOnlyImplementedRules()
    {
        var analyzer = new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer();

        Assert.Equal(
            ["VOSB1002", "VOSB1004"],
            analyzer.SupportedDiagnostics.Select(rule => rule.Id).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "nonserialized-properties-are-ignored")]
    public async Task StaticAndIndexerProperties_AreNotRequiredMessageValuesAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public sealed class Message
    {
        public Guid Id { get; init; }
        public static string Kind => ""message"";
        public string this[int index] => index.ToString();
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(new { Id = Guid.Empty });
        }
    }
}
";

        var diagnostics = await RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        Assert.Empty(diagnostics);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "named-message-argument-is-resolved")]
    public async Task NamedMessageArgument_IsMatchedToItsObjectParameterAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Message
    {
        Guid Id { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(cancellationToken: default, values: new { });
        }
    }
}
";

        var diagnostics = await RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VOSB1004", diagnostic.Id);
        Assert.Contains("properties are missing: Id", diagnostic.Message, StringComparison.Ordinal);
    }
}
