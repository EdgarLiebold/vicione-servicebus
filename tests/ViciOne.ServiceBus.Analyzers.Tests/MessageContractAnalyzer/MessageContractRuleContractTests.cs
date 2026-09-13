using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.MessageContractAnalyzer;

public sealed class MessageContractRuleContractTests
{
    public static TheoryData<string, string> SupportedHeaderCases => new()
    {
        { "__SourceAddress", "new Uri(\"loopback://localhost/source\")" },
        { "__DestinationAddress", "new Uri(\"loopback://localhost/destination\")" },
        { "__ResponseAddress", "new Uri(\"loopback://localhost/response\")" },
        { "__FaultAddress", "new Uri(\"loopback://localhost/fault\")" },
        { "__RequestId", "Guid.Empty" },
        { "__MessageId", "Guid.Empty" },
        { "__ConversationId", "Guid.Empty" },
        { "__CorrelationId", "Guid.Empty" },
        { "__InitiatorId", "Guid.Empty" },
        { "__ScheduledMessageId", "Guid.Empty" },
        { "__TimeToLive", "TimeSpan.Zero" },
        { "__Durable", "true" },
        { "__Header_Tenant", "\"tenant-a\"" },
    };

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "supported-rules-are-executable")]
    public void SupportedDiagnostics_ContainOnlyImplementedRules()
    {
        var analyzer = new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer();

        Assert.Equal(
            ["VOSB1002", "VOSB1004"],
            [.. analyzer.SupportedDiagnostics.Select(rule => rule.Id).Order(StringComparer.Ordinal)]);
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
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "publicly-readable-properties-only")]
    public async Task PropertyWithPrivateGetter_IsNotARequiredMessageValueAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public sealed class Message
    {
        public Guid Id { get; init; }
        public string WriteOnly { private get; init; }
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

        Assert.Empty(await AnalyzeAsync(source));
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
        Assert.Contains("missing properties: Id", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "inherited-contract-properties")]
    public async Task InheritedContractProperties_AreRequiredMessageValuesAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public abstract class MessageBase
    {
        public Guid Id { get; init; }
    }

    public sealed class Message : MessageBase
    {
        public string Name { get; init; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(new { Name = ""order"" });
        }
    }
}
";

        var diagnostics = await AnalyzeAsync(source);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("VOSB1004", diagnostic.Id);
        Assert.Contains("missing properties: Id", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "nonserialized-input-properties")]
    public async Task StaticAndIndexerInputProperties_AreNotMessageValuesAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Message
    {
        Guid Id { get; }
    }

    public sealed class MessageValues
    {
        public Guid Id => Guid.Empty;
        public static string Kind => ""message"";
        public string this[int index] => index.ToString();
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(new MessageValues());
        }
    }
}
";

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Theory]
    [MemberData(nameof(SupportedHeaderCases))]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "supported-message-headers")]
    public async Task SupportedMessageHeader_WithItsCanonicalType_IsAcceptedAsync(
        string headerName,
        string expression)
    {
        var source = ServiceBusAnalyzerFixture.Usings + $$"""
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
                        await bus.PublishAsync<Message>(new
                        {
                            Id = Guid.Empty,
                            {{headerName}} = {{expression}}
                        });
                    }
                }
            }
            """;

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "invalid-message-headers")]
    public async Task UnknownAndInvalidMessageHeaders_AreRejectedAsync()
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
            await bus.PublishAsync<Message>(new
            {
                Id = Guid.Empty,
                __Unknown = ""value"",
                __SourceAddress = 42
            });
        }
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB1002", diagnostic.Id);
        Assert.Contains("__Unknown", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("__SourceAddress", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "recursive-structural-contract")]
    public async Task RecursiveCollectionContract_WithMatchingRecursiveInput_TerminatesWithoutDiagnosticAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Message
    {
        IReadOnlyList<Message> Children { get; }
    }

    public sealed class MessageValues
    {
        public IReadOnlyList<MessageValues> Children { get; init; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(new MessageValues { Children = Array.Empty<MessageValues>() });
        }
    }
}
";

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "destination-schedule-contract")]
    public async Task DestinationSchedule_ValidatesAnonymousMessageValuesAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Message
    {
        Guid Id { get; }
        string Name { get; }
    }

    public sealed class Consumer : IConsumer<Message>
    {
        public async Task ConsumeAsync(ConsumeContext<Message> context)
        {
            await context.Advanced().ScheduleSendAsync<Message>(
                new Uri(""loopback://localhost/input_queue""),
                DateTimeOffset.UtcNow,
                new { Id = Guid.Empty });
        }
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB1004", diagnostic.Id);
        Assert.Contains("missing properties: Name", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "interface-implementation-conversion")]
    public async Task ConcreteInterfaceImplementation_IsACompatibleMessageValueAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Detail
    {
        string Name { get; }
    }

    public sealed class ConcreteDetail : Detail
    {
        public string Name { get; init; }
    }

    public interface Message
    {
        Detail Detail { get; }
    }

    class Program
    {
        static async Task Main()
        {
            var bus = Bus.Factory.CreateUsingInMemory(cfg => { });
            await bus.PublishAsync<Message>(new { Detail = new ConcreteDetail { Name = ""order"" } });
        }
    }
}
";

        Assert.Empty(await AnalyzeAsync(source));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-CONTRACT-ANALYZER", "static-generic-carrier-contract")]
    public async Task StaticGenericInitializerCarrier_ProvidesTheMessageContractAsync()
    {
        var source = ServiceBusAnalyzerFixture.Usings + @"
namespace ConsoleApplication1
{
    public interface Message
    {
        Guid Id { get; }
        string Name { get; }
    }

    class Program
    {
        static async Task Main()
        {
            await ViciOne.ServiceBus.Initializers.MessageInitializerCache<Message>
                .InitializeAsync(new { Id = Guid.Empty });
        }
    }
}
";

        var diagnostic = Assert.Single(await AnalyzeAsync(source));
        Assert.Equal("VOSB1004", diagnostic.Id);
        Assert.Contains("missing properties: Name", diagnostic.Message, StringComparison.Ordinal);
    }

    private static Task<IReadOnlyList<ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics.DiagnosticObservation>> AnalyzeAsync(
        string source) =>
        RoslynTestHost.AnalyzeAsync(
            source,
            new global::ViciOne.ServiceBus.Analyzers.MessageContractAnalyzer(),
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);
}
