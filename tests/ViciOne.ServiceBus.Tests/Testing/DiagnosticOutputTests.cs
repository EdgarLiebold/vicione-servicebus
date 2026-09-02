using System.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class DiagnosticOutputTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "timeline-message-flow")]
    public async Task Timeline_RendersTheProducedAndConsumedMessageFlowWithItsAddress()
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
        harness.Consumer(() => new FlowAConsumer());
        harness.Consumer(() => new FlowBConsumer(harness.InputQueueAddress));
        harness.Consumer(() => new FlowCConsumer());
        harness.Consumer(() => new FlowDConsumer());
        harness.Consumer(() => new FlowEConsumer());

        await harness.Start(cancellationToken);
        try
        {
            var correlationId = Guid.Parse("2eac0a91-a19d-4a43-a502-5dc6fc9e3518");

            await harness.Bus.Publish(new FlowA(correlationId), cancellationToken);
            await harness.Bus.Publish(new FlowB(correlationId), cancellationToken);

            int leafCount = await harness.Consumed
                .SelectAsync<FlowD>(cancellationToken)
                .Take(ExpectedFlowDCount)
                .Count();
            using var writer = new StringWriter();

            await harness.OutputTimeline(writer, options => options.Now().IncludeAddress());

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
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-DIAGNOSTICS", "activity-tree-and-idempotent-disposal")]
    public async Task ActivityListener_RendersTheTraceOnceAndDisposesIdempotently()
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
    public async Task DiagnosticWriters_RejectMissingOutputDestinationsPrecisely()
    {
        ArgumentNullException listener = Assert.Throws<ArgumentNullException>(() =>
            new TestActivityListener(null!, "operation", "Operation", includeDetails: false));
        using var harness = new InMemoryTestHarness();
        ArgumentNullException timeline = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TimelineExtensions.OutputTimeline(harness, null!));

        Assert.Equal("writer", listener.ParamName);
        Assert.Equal("textWriter", timeline.ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private const int ExpectedFlowDCount = 9;

    private sealed class FlowAConsumer : IConsumer<FlowA>
    {
        public async Task Consume(ConsumeContext<FlowA> context)
        {
            await context.Publish(new FlowB(context.Message.CorrelationId));
            await context.Publish(new FlowB(context.Message.CorrelationId));
        }
    }

    private sealed class FlowBConsumer(Uri destinationAddress) : IConsumer<FlowB>
    {
        public async Task Consume(ConsumeContext<FlowB> context)
        {
            await context.Publish(new FlowC(context.Message.CorrelationId));
            ISendEndpoint endpoint = await context.GetSendEndpoint(destinationAddress);
            await endpoint.Send(new FlowE(context.Message.CorrelationId), context.CancellationToken);
        }
    }

    private sealed class FlowCConsumer : IConsumer<FlowC>
    {
        public async Task Consume(ConsumeContext<FlowC> context)
        {
            await context.Publish(new FlowD(context.Message.CorrelationId));
            await context.Publish(new FlowD(context.Message.CorrelationId));
        }
    }

    private sealed class FlowDConsumer : IConsumer<FlowD>
    {
        public Task Consume(ConsumeContext<FlowD> context) => Task.CompletedTask;
    }

    private sealed class FlowEConsumer : IConsumer<FlowE>
    {
        public Task Consume(ConsumeContext<FlowE> context) =>
            context.Publish(new FlowD(context.Message.CorrelationId));
    }

    private sealed record FlowA(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record FlowB(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record FlowC(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record FlowD(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record FlowE(Guid CorrelationId) : CorrelatedBy<Guid>;
}
