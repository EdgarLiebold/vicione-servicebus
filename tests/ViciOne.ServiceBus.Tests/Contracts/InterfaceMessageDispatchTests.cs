using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Contracts;

public sealed class InterfaceMessageDispatchTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INTERFACE-MESSAGE-DISPATCH", "all-implemented-contracts-exactly-once")]
    public async Task ConcreteMessage_IsDeliveredToEveryImplementedInterfaceHandlerExactlyOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"interface-message-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        HandlerTestHarness<FirstMessageContract> first = harness.Handler<FirstMessageContract>();
        HandlerTestHarness<SecondMessageContract> second = harness.Handler<SecondMessageContract>();
        var message = new ConcreteMessage("Joe", 27);
        var stopped = false;

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task<IReceivedMessage<FirstMessageContract>> firstDelivery = first.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IReceivedMessage<SecondMessageContract>> secondDelivery = second.Consumed
                .SelectAsync(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.Advanced().SendAsync((object)message, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await Task.WhenAll(firstDelivery, secondDelivery).WaitAsync(timeout, cancellationToken);
            IReceivedMessage<FirstMessageContract> firstReceived = await firstDelivery;
            IReceivedMessage<SecondMessageContract> secondReceived = await secondDelivery;

            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
            stopped = true;

            FirstMessageContract firstMessage = firstReceived.Context.Message;
            SecondMessageContract secondMessage = secondReceived.Context.Message;
            Assert.True(firstReceived.Context.TryGetMessage(
                out ConsumeContext<SecondMessageContract>? projectedSecond));
            Assert.Equal(message.Name, firstMessage.Name);
            Assert.Equal(message.Name, secondMessage.Name);
            Assert.Equal(message.Age, secondMessage.Age);
            Assert.Equal(message.Name, projectedSecond.Message.Name);
            Assert.Equal(message.Age, projectedSecond.Message.Age);
            Assert.Single(first.Consumed.Select(SnapshotOnlyToken()));
            Assert.Single(second.Consumed.Select(SnapshotOnlyToken()));
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public interface FirstMessageContract
    {
        string Name { get; }
    }

    public interface SecondMessageContract
    {
        string Name { get; }

        int Age { get; }
    }

    private sealed record ConcreteMessage(string Name, int Age) : FirstMessageContract, SecondMessageContract;
}
