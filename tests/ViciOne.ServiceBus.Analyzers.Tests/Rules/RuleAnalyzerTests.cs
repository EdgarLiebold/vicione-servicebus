using Microsoft.CodeAnalysis.Diagnostics;
using ViciOne.ServiceBus.Analyzers.Rules;
using ViciOne.ServiceBus.Analyzers.Tests.Fixtures;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn;
using ViciOne.ServiceBus.Tests.Infrastructure.Roslyn.Diagnostics;
using Xunit;

namespace ViciOne.ServiceBus.Analyzers.Tests.Rules;

public sealed class RuleAnalyzerTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "blocking-only-inside-canonical-consume")]
    public async Task BlockingConsumerAnalyzer_ReportsCanonicalBlockingCallsOnlyInsideConsumeAsync()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            public sealed class Message { }

            public sealed class Consumer : IConsumer<Message>
            {
                public Task ConsumeAsync(ConsumeContext<Message> context)
                {
                    Thread.Sleep(1);
                    Task.Delay(1).Wait();
                    _ = Task.FromResult(1).Result;
                    Task.CompletedTask.GetAwaiter().GetResult();
                    _ = new Foreign.Task().Result;
                    return Task.CompletedTask;
                }

                public void AdministrativeHelper()
                {
                    Thread.Sleep(1);
                    _ = new LookalikeTask().Result;
                }
            }

            public sealed class Unrelated
            {
                public void Run() => Thread.Sleep(1);
            }

            public sealed class LookalikeTask
            {
                public int Result => 42;
            }

            namespace Foreign
            {
                public sealed class Task
                {
                    public int Result => 42;
                }
            }
            """;

        IReadOnlyList<DiagnosticObservation> diagnostics = await AnalyzeAsync(
            new BlockingConsumerCallAnalyzer(),
            source);

        Assert.Equal(4, diagnostics.Count);
        Assert.All(diagnostics, diagnostic => Assert.Equal(BlockingConsumerCallAnalyzer.DiagnosticId, diagnostic.Id));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("Sleep", StringComparison.Ordinal));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("Wait", StringComparison.Ordinal));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("Result", StringComparison.Ordinal));
        Assert.Contains(diagnostics, diagnostic => diagnostic.Message.Contains("GetResult", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "blocking-synchronization-primitives")]
    public async Task BlockingConsumerAnalyzer_ReportsCanonicalSynchronizationPrimitivesAsync()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            public sealed class Message { }

            public sealed class Consumer : IConsumer<Message>
            {
                private readonly object _gate = new();
                private readonly ReaderWriterLockSlim _readerWriterLock = new();

                public Task ConsumeAsync(ConsumeContext<Message> context)
                {
                    lock (_gate) { }
                    Monitor.Enter(_gate);
                    Monitor.TryEnter(_gate);
                    Monitor.TryEnter(_gate, 1);
                    Monitor.Wait(_gate);
                    Monitor.Pulse(_gate);
                    Monitor.Exit(_gate);
                    _readerWriterLock.EnterReadLock();
                    _readerWriterLock.EnterWriteLock();
                    _readerWriterLock.EnterUpgradeableReadLock();
                    _readerWriterLock.TryEnterWriteLock(1);
                    _readerWriterLock.ExitReadLock();
                    SpinWait.SpinUntil(() => true);
                    var spinLock = new SpinLock();
                    var lockTaken = false;
                    spinLock.Enter(ref lockTaken);
                    spinLock.TryEnter(1, ref lockTaken);
                    spinLock.Exit();
                    return Task.CompletedTask;
                }
            }
            """;

        IReadOnlyList<DiagnosticObservation> diagnostics = await AnalyzeAsync(
            new BlockingConsumerCallAnalyzer(),
            source);

        Assert.Equal(11, diagnostics.Count);
        Assert.All(diagnostics, diagnostic => Assert.Equal(BlockingConsumerCallAnalyzer.DiagnosticId, diagnostic.Id));
        Assert.Equal(
            ["Enter", "Enter", "EnterReadLock", "EnterUpgradeableReadLock", "EnterWriteLock", "SpinUntil", "TryEnter", "TryEnter", "TryEnterWriteLock", "Wait", "lock"],
            [.. diagnostics.Select(BlockingOperationName).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "consumer-definition-canonical-properties")]
    public async Task DefinitionAnalyzers_ReportOnlyCanonicalInheritedPropertiesInConsumerDefinitionsAsync()
    {
        const string source = """
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            public sealed class Message { }

            public sealed class Consumer : IConsumer<Message>
            {
                public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
            }

            public sealed class Definition : ConsumerDefinition<Consumer>
            {
                public Definition()
                {
                    ConcurrentMessageLimit = 4;
                    var foreign = new ForeignSettings();
                    foreign.PrefetchCount = 32;
                }

                protected override void ConfigureConsumer(
                    IReceiveEndpointConfigurator endpointConfigurator,
                    IConsumerConfigurator<Consumer> consumerConfigurator,
                    IRegistrationContext context)
                {
                    endpointConfigurator.PrefetchCount = 8;
                }
            }

            public sealed class ForeignSettings
            {
                public int PrefetchCount { get; set; }
            }

            public sealed class ShadowedDefinition : ConsumerDefinition<Consumer>
            {
                protected new int? ConcurrentMessageLimit { get; set; }

                public ShadowedDefinition()
                {
                    ConcurrentMessageLimit = 16;
                }
            }

            public sealed class EndpointOwner
            {
                public void Configure(IReceiveEndpointConfigurator endpoint)
                {
                    endpoint.PrefetchCount = 64;
                }
            }

            namespace Lookalike
            {
                public class ConsumerDefinition<T>
                {
                    protected int? ConcurrentMessageLimit { get; set; }
                }

                public sealed class Definition : ConsumerDefinition<global::Consumer>
                {
                    public Definition()
                    {
                        ConcurrentMessageLimit = 2;
                    }
                }
            }
            """;

        IReadOnlyList<DiagnosticObservation> qos = await AnalyzeAsync(new ConsumerEndpointQosAnalyzer(), source);
        IReadOnlyList<DiagnosticObservation> concurrency = await AnalyzeAsync(
            new ConsumerConcurrencyDeclarationAnalyzer(),
            source);

        DiagnosticObservation qosDiagnostic = Assert.Single(qos);
        Assert.Equal(ConsumerEndpointQosAnalyzer.DiagnosticId, qosDiagnostic.Id);
        Assert.Contains("PrefetchCount", qosDiagnostic.Message, StringComparison.Ordinal);
        DiagnosticObservation concurrencyDiagnostic = Assert.Single(concurrency);
        Assert.Equal(ConsumerConcurrencyDeclarationAnalyzer.DiagnosticId, concurrencyDiagnostic.Id);
        Assert.Contains("ConcurrentMessageLimit", concurrencyDiagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "all-configuration-write-forms")]
    public async Task DefinitionAnalyzers_ReportEveryCanonicalConfigurationWriteFormAsync()
    {
        const string source = """
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            public sealed class Message { }

            public sealed class Consumer : IConsumer<Message>
            {
                public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
            }

            public sealed class Definition : ConsumerDefinition<Consumer>
            {
                public Definition()
                {
                    ConcurrentMessageLimit += 1;
                    ConcurrentMessageLimit ??= 1;
                    ++ConcurrentMessageLimit;
                }

                protected override void ConfigureConsumer(
                    IReceiveEndpointConfigurator endpointConfigurator,
                    IConsumerConfigurator<Consumer> consumerConfigurator,
                    IRegistrationContext context)
                {
                    endpointConfigurator.PrefetchCount += 1;
                    endpointConfigurator.PrefetchCount++;
                }
            }
            """;

        IReadOnlyList<DiagnosticObservation> qos = await AnalyzeAsync(new ConsumerEndpointQosAnalyzer(), source);
        IReadOnlyList<DiagnosticObservation> concurrency = await AnalyzeAsync(
            new ConsumerConcurrencyDeclarationAnalyzer(),
            source);

        Assert.Equal(2, qos.Count);
        Assert.All(qos, diagnostic => Assert.Equal(ConsumerEndpointQosAnalyzer.DiagnosticId, diagnostic.Id));
        Assert.Equal(3, concurrency.Count);
        Assert.All(concurrency, diagnostic => Assert.Equal(ConsumerConcurrencyDeclarationAnalyzer.DiagnosticId, diagnostic.Id));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "excluded-topology-canonical-contract")]
    public async Task ExcludedTopologyAnalyzer_ReportsOnlyCanonicalExcludedConsumedContractsAsync()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            [ExcludeFromTopology]
            public interface ExcludedMessage { }

            public interface NormalMessage { }

            [Lookalike.ExcludeFromTopology]
            public interface LookalikeExcludedMessage { }

            public sealed class ExcludedConsumer : IConsumer<ExcludedMessage>
            {
                public Task ConsumeAsync(ConsumeContext<ExcludedMessage> context) => Task.CompletedTask;
            }

            public sealed class NormalConsumer : IConsumer<NormalMessage>
            {
                public Task ConsumeAsync(ConsumeContext<NormalMessage> context) => Task.CompletedTask;
            }

            public sealed class LookalikeConsumer : IConsumer<LookalikeExcludedMessage>
            {
                public Task ConsumeAsync(ConsumeContext<LookalikeExcludedMessage> context) => Task.CompletedTask;
            }

            namespace Lookalike
            {
                [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
                public sealed class ExcludeFromTopologyAttribute : Attribute { }
            }
            """;

        IReadOnlyList<DiagnosticObservation> diagnostics = await AnalyzeAsync(
            new ExcludedTopologyConsumerAnalyzer(),
            source);

        DiagnosticObservation diagnostic = Assert.Single(diagnostics);
        Assert.Equal(ExcludedTopologyConsumerAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Contains("ExcludedConsumer", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("ExcludedMessage", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "large-inline-inherited-consumed-and-outbound-contracts")]
    public async Task LargeInlinePayloadAnalyzer_CoversInheritedConsumedAndOutboundContractsWithoutMessageDataFalsePositiveAsync()
    {
        const string source = """
            using System;
            using System.IO;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            public interface PayloadBase
            {
                byte[] Bytes { get; }
                Memory<byte> Memory { get; }
                ReadOnlyMemory<byte> ReadOnlyMemory { get; }
                Stream Stream { get; }
                MessageData<byte[]> External { get; }
            }

            public interface IncomingPayload : PayloadBase { }

            public sealed class Consumer : IConsumer<IncomingPayload>
            {
                public Task ConsumeAsync(ConsumeContext<IncomingPayload> context) => Task.CompletedTask;
            }

            public abstract class OutboundBase
            {
                public byte[] Attachment { get; init; } = [];
            }

            [MessageContract("outbound-payload")]
            public sealed class OutboundPayload : OutboundBase
            {
                public MessageData<byte[]> External { get; init; }
            }
            """;

        IReadOnlyList<DiagnosticObservation> diagnostics = await AnalyzeAsync(
            new LargeInlinePayloadAnalyzer(),
            source);

        Assert.Equal(5, diagnostics.Count);
        Assert.All(diagnostics, diagnostic => Assert.Equal(LargeInlinePayloadAnalyzer.DiagnosticId, diagnostic.Id));
        Assert.Equal(
            ["Attachment", "Bytes", "Memory", "ReadOnlyMemory", "Stream"],
            [.. diagnostics.Select(MemberName).Order(StringComparer.Ordinal)]);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Message.Contains("External", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "large-inline-serialized-members-only")]
    public async Task LargeInlinePayloadAnalyzer_ReportsEachSerializedMemberExactlyOnceAsync()
    {
        const string source = """
            using ViciOne.ServiceBus;

            public interface PayloadContract
            {
                byte[] Data { get; }
            }

            [MessageContract("payload")]
            public sealed class Payload : PayloadContract
            {
                private byte[] PrivateData { get; } = [];
                protected byte[] ProtectedData { get; } = [];
                public static byte[] StaticData { get; } = [];
                public byte[] this[int index] => [];
                public byte[] WriteOnlyData { set { } }
                public byte[] PublicField = [];
                public byte[] Data { get; init; } = [];
            }
            """;

        IReadOnlyList<DiagnosticObservation> diagnostics = await AnalyzeAsync(
            new LargeInlinePayloadAnalyzer(),
            source);

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(
            ["Data", "PublicField"],
            [.. diagnostics.Select(MemberName).Order(StringComparer.Ordinal)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "generated-code-is-excluded")]
    public async Task EveryRuleAnalyzer_IgnoresGeneratedCodeAsync()
    {
        const string source = """
            // <auto-generated/>
            using System.Threading;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            [ExcludeFromTopology]
            public interface GeneratedMessage
            {
                byte[] Data { get; }
            }

            public sealed class GeneratedConsumer : IConsumer<GeneratedMessage>
            {
                public Task ConsumeAsync(ConsumeContext<GeneratedMessage> context)
                {
                    Thread.Sleep(1);
                    return Task.CompletedTask;
                }
            }

            public sealed class GeneratedDefinition : ConsumerDefinition<GeneratedConsumer>
            {
                public GeneratedDefinition()
                {
                    ConcurrentMessageLimit = 2;
                }

                protected override void ConfigureConsumer(
                    IReceiveEndpointConfigurator endpointConfigurator,
                    IConsumerConfigurator<GeneratedConsumer> consumerConfigurator,
                    IRegistrationContext context)
                {
                    endpointConfigurator.PrefetchCount = 8;
                }
            }
            """;

        foreach (DiagnosticAnalyzer analyzer in CreateAnalyzers())
            Assert.Empty(await AnalyzeAsync(analyzer, source));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RULE-ANALYZERS", "parallel-deterministic-shared-instance")]
    public async Task EveryRuleAnalyzer_IsDeterministicWhenOneInstanceAnalyzesInParallelAsync()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using ViciOne.ServiceBus;

            [ExcludeFromTopology]
            public interface RiskyMessage
            {
                byte[] Data { get; }
            }

            public sealed class Consumer : IConsumer<RiskyMessage>
            {
                public Task ConsumeAsync(ConsumeContext<RiskyMessage> context)
                {
                    Thread.Sleep(1);
                    return Task.CompletedTask;
                }
            }

            public sealed class SecondConsumer : IConsumer<RiskyMessage>
            {
                public Task ConsumeAsync(ConsumeContext<RiskyMessage> context) => Task.CompletedTask;
            }

            public sealed class Definition : ConsumerDefinition<Consumer>
            {
                public Definition()
                {
                    ConcurrentMessageLimit = 2;
                }

                protected override void ConfigureConsumer(
                    IReceiveEndpointConfigurator endpointConfigurator,
                    IConsumerConfigurator<Consumer> consumerConfigurator,
                    IRegistrationContext context)
                {
                    endpointConfigurator.PrefetchCount = 8;
                }
            }
            """;

        foreach (DiagnosticAnalyzer analyzer in CreateAnalyzers())
        {
            Task<IReadOnlyList<DiagnosticObservation>>[] runs = [.. Enumerable.Range(0, 8).Select(_ => AnalyzeAsync(analyzer, source))];
            IReadOnlyList<DiagnosticObservation>[] results = await Task.WhenAll(runs);

            Assert.NotEmpty(results[0]);
            Assert.All(results.Skip(1), result => Assert.Equal(results[0], result));
            if (analyzer is LargeInlinePayloadAnalyzer)
                Assert.Single(results[0]);
        }
    }

    private static DiagnosticAnalyzer[] CreateAnalyzers() =>
    [
        new BlockingConsumerCallAnalyzer(),
        new ConsumerEndpointQosAnalyzer(),
        new ConsumerConcurrencyDeclarationAnalyzer(),
        new ExcludedTopologyConsumerAnalyzer(),
        new LargeInlinePayloadAnalyzer(),
    ];

    private static Task<IReadOnlyList<DiagnosticObservation>> AnalyzeAsync(
        DiagnosticAnalyzer analyzer,
        string source) =>
        RoslynTestHost.AnalyzeAsync(
            source,
            analyzer,
            ServiceBusAnalyzerFixture.ReferenceRoots,
            TestContext.Current.CancellationToken);

    private static string MemberName(DiagnosticObservation diagnostic)
    {
        const string marker = "Message contract member '";
        int start = diagnostic.Message.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = diagnostic.Message.IndexOf('\'', start);
        return diagnostic.Message[start..end];
    }

    private static string BlockingOperationName(DiagnosticObservation diagnostic)
    {
        const string marker = "blocking call '";
        int start = diagnostic.Message.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        int end = diagnostic.Message.IndexOf('\'', start);
        return diagnostic.Message[start..end];
    }
}
