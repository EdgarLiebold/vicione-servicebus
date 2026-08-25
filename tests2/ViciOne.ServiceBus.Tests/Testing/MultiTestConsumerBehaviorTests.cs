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
    public async Task MultipleContracts_AreRecordedInTheirTypedAndAggregateLists()
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
        ReceivedMessageList<FirstMessage> firstMessages = consumer.Consume<FirstMessage>();
        ReceivedMessageList<SecondMessage> secondMessages = consumer.Consume<SecondMessage>();
        harness.OnConfigureInMemoryReceiveEndpoint += consumer.Configure;

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new FirstMessage("first"), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new SecondMessage("second"), cancellationToken);

            IReceivedMessage<FirstMessage> first = await firstMessages.SelectAsync(cancellationToken).First();
            IReceivedMessage<SecondMessage> second = await secondMessages.SelectAsync(cancellationToken).First();
            IReceivedMessage[] aggregate = consumer.Received.Select(_ => true, cancellationToken).Take(2).ToArray();

            Assert.Equal("first", first.Context.Message.Value);
            Assert.Equal("second", second.Context.Message.Value);
            Assert.Null(first.Exception);
            Assert.Null(second.Exception);
            Assert.Equal(2, aggregate.Length);
            Assert.Contains(aggregate, message => message.MessageObject is FirstMessage { Value: "first" });
            Assert.Contains(aggregate, message => message.MessageObject is SecondMessage { Value: "second" });
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MULTI-CONSUMER", "intentional-fault-is-visible-to-pipeline")]
    public async Task FaultContract_RecordsTheMessageAndFaultsTheReceivePipeline()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"multi-consumer-fault-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var consumer = new MultiTestConsumer(timeout, harness.InactivityToken);
        ReceivedMessageList<FaultMessage> faultMessages = consumer.Fault<FaultMessage>();
        harness.OnConfigureInMemoryReceiveEndpoint += consumer.Configure;

        await harness.Start(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.Send(new FaultMessage("fault"), cancellationToken);

            IReceivedMessage<FaultMessage> consumerObservation = await faultMessages
                .SelectAsync(cancellationToken)
                .First();
            IReceivedMessage<FaultMessage> pipelineObservation = await harness.Consumed
                .SelectAsync<FaultMessage>(cancellationToken)
                .First();

            Assert.Equal("fault", consumerObservation.Context.Message.Value);
            Assert.Null(consumerObservation.Exception);
            InvalidOperationException exception = Assert.IsType<InvalidOperationException>(pipelineObservation.Exception);
            Assert.Equal("This is intentional from a test", exception.Message);
        }
        finally
        {
            await harness.Stop();
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record FirstMessage(string Value);

    private sealed record SecondMessage(string Value);

    private sealed record FaultMessage(string Value);
}
