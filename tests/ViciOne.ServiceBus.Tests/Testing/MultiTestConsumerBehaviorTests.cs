using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MultiTestConsumerBehaviorTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MULTI-CONSUMER", "typed-and-aggregate-observations")]
    public async Task MultipleContracts_AreRecordedInTheirTypedAndAggregateListsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new InMemoryTestHarness(timeProvider, $"multi-consumer-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumer = new MultiTestConsumer(timeout, timeProvider, harness.InactivityToken);
        IConsumedMessageList<FirstMessage> firstMessages = consumer.AddConsumer<FirstMessage>();
        IConsumedMessageList<SecondMessage> secondMessages = consumer.AddConsumer<SecondMessage>();
        harness.InMemoryReceiveEndpointConfiguring += consumer.Configure;

        await harness.StartAsync(cancellationToken);
        try
        {
            var firstCorrelationId = NewId.NextGuid();
            var repeatedCorrelationId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.SendAsync(
                new FirstMessage(firstCorrelationId, "first"),
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(
                new FirstMessage(repeatedCorrelationId, "repeated"),
                cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new SecondMessage("second"), cancellationToken);

            Assert.Equal(2, await firstMessages
                .SelectAsync(cancellationToken)
                .Take(2)
                .CountObservedAsync(TestContext.Current.CancellationToken));
            IConsumedMessage<FirstMessage>[] first = firstMessages.Snapshot().ToArray();
            IConsumedMessage<SecondMessage> second = await secondMessages.SelectAsync(cancellationToken).FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumedMessage[] aggregate = consumer.Consumed.Snapshot().Take(3).ToArray();

            Assert.Equal(
                new[] { firstCorrelationId, repeatedCorrelationId }.Order(),
                first.Select(message => message.Context.Message.CorrelationId).Order());
            Assert.Equal(["first", "repeated"], first.Select(message => message.Context.Message.Value).Order());
            Assert.Equal("second", second.Context.Message.Value);
            Assert.All(first, message => Assert.Null(message.Exception));
            Assert.Null(second.Exception);
            Assert.Equal(3, aggregate.Length);
            Assert.Contains(aggregate, message => message.MessageObject is FirstMessage { Value: "first" });
            Assert.Contains(aggregate, message => message.MessageObject is FirstMessage { Value: "repeated" });
            Assert.Contains(aggregate, message => message.MessageObject is SecondMessage { Value: "second" });
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MULTI-CONSUMER", "intentional-fault-is-visible-to-pipeline")]
    public async Task FaultContract_RecordsTheMessageAndFaultsTheReceivePipelineAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"multi-consumer-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumer = new MultiTestConsumer(timeout, harness.InactivityToken);
        IConsumedMessageList<FaultMessage> faultMessages = consumer.AddFaultingConsumer<FaultMessage>();
        harness.InMemoryReceiveEndpointConfiguring += consumer.Configure;

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new FaultMessage("fault"), cancellationToken);

            IConsumedMessage<FaultMessage> consumerObservation = await faultMessages
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IConsumedMessage<FaultMessage> pipelineObservation = await harness.Consumed
                .SelectAsync<FaultMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("fault", consumerObservation.Context.Message.Value);
            Assert.Null(consumerObservation.Exception);
            InvalidOperationException exception = Assert.IsType<InvalidOperationException>(pipelineObservation.Exception);
            Assert.Equal("The configured test consumer faulted intentionally.", exception.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MULTI-CONSUMER", "direct-pipe-connection")]
    public async Task DirectPipeConnection_AttachesEveryConfiguredConsumerAndRejectsANullConnectorAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"multi-consumer-direct-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumer = new MultiTestConsumer(timeout, harness.InactivityToken);
        IConsumedMessageList<DirectMessage> messages = consumer.AddConsumer<DirectMessage>();

        Assert.Equal("connector", Assert.Throws<ArgumentNullException>(() => consumer.Connect(null!)).ParamName);

        await harness.StartAsync(cancellationToken);
        try
        {
            using ConnectHandle connection = consumer.Connect((IConsumePipeConnector)harness.Bus);
            var expected = new DirectMessage(NewId.NextGuid(), "direct");
            ISendEndpoint endpoint = await harness.Bus.GetSendEndpointAsync(
                harness.Bus.Address,
                TestContext.Current.CancellationToken);

            await endpoint.SendAsync(expected, cancellationToken);

            IConsumedMessage<DirectMessage> observed = await messages
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken);
            Assert.Equal(expected, observed.Context.Message);
            Assert.Null(observed.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record FirstMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record SecondMessage(string Value);

    private sealed record FaultMessage(string Value);

    private sealed record DirectMessage(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;
}
