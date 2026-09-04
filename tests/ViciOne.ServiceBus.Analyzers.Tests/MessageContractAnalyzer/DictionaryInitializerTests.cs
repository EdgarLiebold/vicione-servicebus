using Microsoft.CodeAnalysis;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class DictionaryInitializerTests
{
    private const string Contracts = @"
namespace ConsoleApplication1
{
    public interface OrderSubmitted
    {
        Guid Id { get; }
        string CustomerId { get; }
        IReadOnlyDictionary<string, OrderItem> OrderItems { get; }
    }

    public interface SubmitOrder
    {
        Guid Id { get; }
        string CustomerId { get; }
        IReadOnlyList<OrderItem> OrderItems { get; }
    }

    public interface OrderItem
    {
        Guid Id { get; }
        Product Product { get; }
        int Quantity { get; }
        decimal Price { get; }
    }

    public interface Product
    {
        string Name { get; }
        Uri Category { get; }
    }
}
";

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "dictionary-missing-properties")]
    public async Task EmptyAnonymousValue_ReportsEveryMissingDictionaryContractPropertyAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<OrderSubmitted>(new
            {
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "MCA0003",
                DiagnosticSeverity.Info,
                "Anonymous type is missing properties that are in the message contract 'OrderSubmitted'. The following properties are missing: Id, CustomerId, OrderItems.",
                "Test0.cs",
                47,
                52));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "dictionary-incompatible-value")]
    public async Task ArrayInsteadOfDictionary_ReportsTheIncompatibleContractPropertyAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<OrderSubmitted>(new
            {
                InVar.Id,
                CustomerId = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E7"",
                OrderItems = new []
                {
                    new
                    {
                        Id = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E9"",
                        Product = new
                        {
                            Name = ""Pencil"",
                            Category = ""category:office""
                        },
                        Quantity = 10,
                        Price = 10.0m
                    }
                }
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(
            source,
            new DiagnosticObservation(
                "MCA0001",
                DiagnosticSeverity.Error,
                "Anonymous type does not map to message contract 'OrderSubmitted'. The following properties of the anonymous type are incompatible: OrderItems.",
                "Test0.cs",
                47,
                52));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "dictionary-compatible-value")]
    public async Task DictionaryProjection_SatisfiesTheDictionaryContractAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + Contracts + @"
namespace ConsoleApplication1
{
    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });

            await bus.PublishAsync<OrderSubmitted>(new
            {
                InVar.Id,
                CustomerId = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E7"",
                OrderItems = new []
                {
                    new
                    {
                        Id = ""53051996-AEEC-4EF1-BCFD-7835F17BA8E9"",
                        Product = new
                        {
                            Name = ""Pencil"",
                            Category = ""category:office""
                        },
                        Quantity = 10,
                        Price = 10.0m
                    }
                }.ToDictionary(x => x.Id)
            });
        }
    }
}
";

        await AssertDiagnosticsAsync(source);
    }

    private static async Task AssertDiagnosticsAsync(
        string source,
        params DiagnosticObservation[] expected)
    {
        var actual = await RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

        ServiceBusAnalyzerFixture.AssertDiagnostics(actual, expected);
    }
}
