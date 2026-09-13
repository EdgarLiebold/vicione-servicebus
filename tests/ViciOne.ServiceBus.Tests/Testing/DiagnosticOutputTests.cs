using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class DiagnosticOutputTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "timeline-message-flow")]
    public async Task Timeline_RendersTheProducedAndConsumedMessageFlowWithItsAddressAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness(
            new FakeTimeProvider(new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)),
            $"timeline-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.AddConsumer(() => new FlowAConsumer());
        harness.AddConsumer(() => new FlowBConsumer(harness.InputQueueAddress));
        harness.AddConsumer(() => new FlowCConsumer());
        harness.AddConsumer(() => new FlowDConsumer());
        harness.AddConsumer(() => new FlowEConsumer());

        await harness.StartAsync(cancellationToken);
        try
        {
            var correlationId = Guid.Parse("2eac0a91-a19d-4a43-a502-5dc6fc9e3518");

            await harness.Bus.PublishAsync(new FlowA(correlationId), cancellationToken);
            await harness.Bus.PublishAsync(new FlowB(correlationId), cancellationToken);

            int leafCount = await harness.Consumed
                .SelectAsync<FlowD>(cancellationToken)
                .Take(ExpectedFlowDCount)
                .CountObservedAsync(TestContext.Current.CancellationToken);
            using var writer = new StringWriter();

            await harness.OutputTimelineAsync(writer, options => options.RenderImmediately().IncludeEndpointAddress(),
                cancellationToken: TestContext.Current.CancellationToken);

            string output = writer.ToString();
            string[] lines = output.Split('\n');
            static int Rows(IEnumerable<string> rows, string operation) =>
                rows.Count(row => row.Contains(operation, StringComparison.Ordinal));

            Assert.Equal(ExpectedFlowDCount, leafCount);
            Assert.Contains("Operation", output, StringComparison.Ordinal);
            Assert.Contains("Address", output, StringComparison.Ordinal);
            Assert.Equal(1, Rows(lines, "Publish FlowA"));
            Assert.Equal(3, Rows(lines, "Publish FlowB"));
            Assert.Equal(3, Rows(lines, "Publish FlowC"));
            Assert.Equal(3, Rows(lines, "Send FlowE"));
            Assert.Equal(ExpectedFlowDCount, Rows(lines, "Publish FlowD"));
            Assert.Equal(1, Rows(lines, "Consume FlowA"));
            Assert.Equal(3, Rows(lines, "Consume FlowB"));
            Assert.Equal(3, Rows(lines, "Consume FlowC"));
            Assert.Equal(3, Rows(lines, "Consume FlowE"));
            Assert.Equal(ExpectedFlowDCount, Rows(lines, "Consume FlowD"));
            Assert.All(lines.Where(line => line.Contains("Consume ", StringComparison.Ordinal)),
                line => Assert.Contains(harness.InputQueueName, line, StringComparison.Ordinal));

            using var namespacedWriter = new StringWriter();
            await harness.OutputTimelineAsync(
                namespacedWriter,
                options => options.RenderImmediately().IncludeMessageNamespace(),
                cancellationToken: TestContext.Current.CancellationToken);
            Assert.Contains("ViciOne.ServiceBus.Tests.Testing", namespacedWriter.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "activity-tree-and-idempotent-disposal")]
    public async Task ActivityListener_RendersTheTraceOnceAndDisposesIdempotentlyAsync()
    {
        using var writer = new StringWriter();
        var listener = new TestActivityListener(writer, "root-operation", "Operation", includeDetails: true);
        using var source = new ActivitySource("ViciOne.ServiceBus.Tests.DiagnosticOutput");

        using (Activity? activity = source.StartActivity("child-operation"))
        {
            Assert.NotNull(activity);
            activity.SetTag("test.value", "observed");
        }

        await listener.DisposeAsync();
        string firstOutput = writer.ToString();
        await listener.DisposeAsync();

        Assert.Contains("Operation", firstOutput, StringComparison.Ordinal);
        Assert.Contains("Details", firstOutput, StringComparison.Ordinal);
        Assert.Contains("root-operation", firstOutput, StringComparison.Ordinal);
        Assert.Contains("child-operation", firstOutput, StringComparison.Ordinal);
        Assert.Equal(firstOutput, writer.ToString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "required-output-destinations")]
    public async Task DiagnosticWriters_RejectMissingOutputDestinationsPreciselyAsync()
    {
        ArgumentNullException listener = Assert.Throws<ArgumentNullException>(() =>
            new TestActivityListener(null!, "operation", "Operation", includeDetails: false));
        using var harness = new InMemoryTestHarness();
        ArgumentNullException timeline = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TimelineExtensions.OutputTimelineAsync(harness, null!, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal("writer", listener.ParamName);
        Assert.Equal("textWriter", timeline.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "cyclic-parent-metadata")]
    public async Task Timeline_RendersEachProducedMessageOnceWhenParentMetadataContainsACycleAsync()
    {
        TimeSpan timeout = OperationTimeout();
        Guid conversationId = NewId.NextGuid();
        Guid firstId = NewId.NextGuid();
        Guid secondId = NewId.NextGuid();
        using var observationsCompleted = new CancellationTokenSource();
        var sent = new SentMessageList(timeout, observationsCompleted.Token);
        sent.Add(CreateCyclicContext(new CyclicTimelineMessage("first"), firstId, secondId, conversationId));
        sent.Add(CreateCyclicContext(new CyclicTimelineMessage("second"), secondId, firstId, conversationId));
        observationsCompleted.Cancel();
        var harness = new TimelineHarness(sent, timeout, observationsCompleted.Token);
        using var writer = new StringWriter();
        using var cancellation = new CancellationTokenSource(timeout);

        Task rendering = Task.Run(
            () => harness.OutputTimelineAsync(writer, cancellationToken: cancellation.Token),
            CancellationToken.None);
        await rendering.WaitAsync(timeout, TestContext.Current.CancellationToken);

        string[] lines = writer.ToString().Split('\n');
        Assert.Equal(2, lines.Count(line => line.Contains("Send CyclicTimelineMessage", StringComparison.Ordinal)));
    }

    private static MessageSendContext<CyclicTimelineMessage> CreateCyclicContext(
        CyclicTimelineMessage message,
        Guid messageId,
        Guid parentMessageId,
        Guid conversationId)
    {
        var context = new MessageSendContext<CyclicTimelineMessage>(message)
        {
            MessageId = messageId,
            ConversationId = conversationId,
            DestinationAddress = new Uri("loopback://localhost/timeline"),
        };
        ConsumeContext parent = DispatchProxy.Create<ConsumeContext, ParentContextProxy>();
        ((ParentContextProxy)(object)parent).MessageId = parentMessageId;
        context.GetOrAddPayload(() => parent);
        return context;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private const int ExpectedFlowDCount = 9;

    private sealed class FlowAConsumer : IConsumer<FlowA>
    {
        public async Task ConsumeAsync(ConsumeContext<FlowA> context)
        {
            await context.Advanced().PublishAsync(new FlowB(context.Message.CorrelationId));
            await context.Advanced().PublishAsync(new FlowB(context.Message.CorrelationId));
        }
    }

    private sealed class FlowBConsumer(Uri destinationAddress) : IConsumer<FlowB>
    {
        public async Task ConsumeAsync(ConsumeContext<FlowB> context)
        {
            await context.Advanced().PublishAsync(new FlowC(context.Message.CorrelationId));
            ISendEndpoint endpoint = await context.Advanced().GetSendEndpointAsync(destinationAddress);
            await endpoint.SendAsync(new FlowE(context.Message.CorrelationId), context.CancellationToken);
        }
    }

    private sealed class FlowCConsumer : IConsumer<FlowC>
    {
        public async Task ConsumeAsync(ConsumeContext<FlowC> context)
        {
            await context.Advanced().PublishAsync(new FlowD(context.Message.CorrelationId));
            await context.Advanced().PublishAsync(new FlowD(context.Message.CorrelationId));
        }
    }

    private sealed class FlowDConsumer : IConsumer<FlowD>
    {
        public Task ConsumeAsync(ConsumeContext<FlowD> context) => Task.CompletedTask;
    }

    private sealed class FlowEConsumer : IConsumer<FlowE>
    {
        public Task ConsumeAsync(ConsumeContext<FlowE> context) =>
            context.Advanced().PublishAsync(new FlowD(context.Message.CorrelationId));
    }

    private sealed record FlowA(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record FlowB(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record FlowC(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record FlowD(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record FlowE(Guid CorrelationId) : ICorrelatedBy<Guid>;

    private sealed record CyclicTimelineMessage(string Value);

    private sealed class TimelineHarness(ISentMessageList sent, TimeSpan timeout, CancellationToken observationsCompleted) : IBaseTestHarness
    {
        public TimeSpan TestTimeout { get; set; } = timeout;
        public TimeSpan TestInactivityTimeout { get; set; } = timeout;
        public TimeProvider TimeProvider => TimeProvider.System;
        public TestContextSaveMode ContextSaveMode => TestContextSaveMode.All;
        public int MaximumSavedContexts => int.MaxValue;
        public CancellationToken CancellationToken => CancellationToken.None;
        public CancellationToken InactivityToken => observationsCompleted;
        public Task InactivityTask => Task.CompletedTask;
        public IConsumedMessageList Consumed { get; } = new ConsumedMessageList(timeout, observationsCompleted);
        public IPublishedMessageList Published { get; } = new PublishedMessageList(timeout, observationsCompleted);
        public ISentMessageList Sent { get; } = sent;

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => throw new NotSupportedException();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => throw new NotSupportedException();

        public void Cancel() => throw new NotSupportedException();

        public void ForceInactive()
        {
        }
    }

    private class ParentContextProxy : DispatchProxy
    {
        public Guid MessageId { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name == "get_MessageId"
                ? MessageId
                : throw new NotSupportedException(targetMethod.Name);
        }
    }
}
