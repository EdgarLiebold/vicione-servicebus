using System.Collections.Concurrent;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryTransportIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INMEMORY-TRANSPORT-ISOLATION", "cross-host-relay-preserves-source-and-prevents-loop")]
    public async Task DistinctVirtualHosts_RequireOneExplicitRelayAndPreserveTheOriginalSourceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string internalVirtualHost = $"internal-{NewId.NextGuid():N}";
        string externalVirtualHost = $"external-{NewId.NextGuid():N}";
        var internalRelayDecisions = new ConcurrentQueue<RelayDecision>();
        var externalRelayDecisions = new ConcurrentQueue<RelayDecision>();
        var realDeliveries = new ConcurrentQueue<Delivery>();
        using var internalHarness = CreateHarness(timeout, internalVirtualHost);
        using var externalHarness = CreateHarness(timeout, externalVirtualHost);
        Assert.Equal(new Uri($"loopback://localhost/{internalVirtualHost}/"), internalHarness.BaseAddress);
        Assert.Equal(new Uri($"loopback://localhost/{externalVirtualHost}/"), externalHarness.BaseAddress);
        Assert.NotEqual(internalHarness.BaseAddress, externalHarness.BaseAddress);
        ConsumerTestHarness<RelayConsumer> internalRelay = internalHarness.Consumer(
            () => new RelayConsumer(externalHarness.Bus, internalRelayDecisions));
        ConsumerTestHarness<RelayConsumer> externalRelay = externalHarness.Consumer(
            () => new RelayConsumer(internalHarness.Bus, externalRelayDecisions));
        ConsumerTestHarness<RealConsumer> realConsumer = internalHarness.Consumer(
            () => new RealConsumer(realDeliveries));
        bool internalStarted = false;
        bool externalStarted = false;

        try
        {
            await internalHarness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            internalStarted = true;
            await externalHarness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            externalStarted = true;

            Uri externalBusAddress = externalHarness.BusAddress;
            Guid messageId = Guid.Parse("56d68b7c-d828-4548-b66d-08f0091382d0");
            var message = new IsolationMessage(Guid.Parse("3111f42a-648a-49da-ad71-c63a9494eb80"));
            Task<IReceivedMessage<IsolationMessage>> externalRelayObserved = externalRelay.Consumed
                .SelectAsync<IsolationMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IReceivedMessage<IsolationMessage>> internalRelayObserved = internalRelay.Consumed
                .SelectAsync<IsolationMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IReceivedMessage<IsolationMessage>> realConsumerObserved = realConsumer.Consumed
                .SelectAsync<IsolationMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await externalHarness.Bus.PublishAsync(
                    message,
                    context => context.MessageId = messageId,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            IReceivedMessage<IsolationMessage>[] observations = await Task.WhenAll(
                    externalRelayObserved,
                    internalRelayObserved,
                    realConsumerObserved)
                .WaitAsync(timeout, cancellationToken);

            await externalHarness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            externalStarted = false;
            await internalHarness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
            internalStarted = false;

            Assert.Equal(messageId, observations[0].Context.MessageId);
            Assert.Equal(messageId, observations[1].Context.MessageId);
            Assert.Equal(messageId, observations[2].Context.MessageId);
            Assert.Equal(externalBusAddress, observations[0].Context.SourceAddress);
            Assert.Equal(externalBusAddress, observations[1].Context.SourceAddress);
            Assert.Equal(externalBusAddress, observations[2].Context.SourceAddress);
            Assert.Equal(externalHarness.InputQueueAddress, observations[0].Context.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(internalHarness.InputQueueAddress, observations[1].Context.Advanced().ReceiveContext.InputAddress);
            Assert.Equal(internalHarness.InputQueueAddress, observations[2].Context.Advanced().ReceiveContext.InputAddress);

            RelayDecision externalDecision = Assert.Single(externalRelayDecisions);
            Assert.True(externalDecision.Forwarded);
            Assert.Equal(externalVirtualHost, externalDecision.SourceVirtualHost);
            Assert.Equal(externalVirtualHost, externalDecision.InputVirtualHost);
            Assert.Equal(messageId, externalDecision.MessageId);

            RelayDecision internalDecision = Assert.Single(internalRelayDecisions);
            Assert.False(internalDecision.Forwarded);
            Assert.Equal(externalVirtualHost, internalDecision.SourceVirtualHost);
            Assert.Equal(internalVirtualHost, internalDecision.InputVirtualHost);
            Assert.Equal(messageId, internalDecision.MessageId);

            Delivery delivery = Assert.Single(realDeliveries);
            Assert.Equal(message.Token, delivery.Token);
            Assert.Equal(messageId, delivery.MessageId);
            Assert.Equal(externalBusAddress, delivery.SourceAddress);
            Assert.Equal(internalHarness.InputQueueAddress, delivery.InputAddress);
            Assert.Single(externalRelay.Consumed.Select<IsolationMessage>(SnapshotOnlyToken()));
            Assert.Single(internalRelay.Consumed.Select<IsolationMessage>(SnapshotOnlyToken()));
            Assert.Single(realConsumer.Consumed.Select<IsolationMessage>(SnapshotOnlyToken()));
        }
        finally
        {
            if (externalStarted)
                await externalHarness.StopAsync(TestContext.Current.CancellationToken);
            if (internalStarted)
                await internalHarness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, string virtualHost) =>
        new(virtualHost)
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static string GetVirtualHost(Uri address) => address.AbsolutePath
        .Split('/', StringSplitOptions.RemoveEmptyEntries)
        .First();

    private sealed record IsolationMessage(Guid Token);

    private sealed record RelayDecision(
        Guid? MessageId,
        string SourceVirtualHost,
        string InputVirtualHost,
        bool Forwarded);

    private sealed record Delivery(Guid? MessageId, Guid Token, Uri SourceAddress, Uri InputAddress);

    private sealed class RelayConsumer(
        IPublishEndpoint otherHost,
        ConcurrentQueue<RelayDecision> decisions) : IConsumer<IsolationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IsolationMessage> context)
        {
            Uri sourceAddress = context.SourceAddress
                ?? throw new InvalidOperationException("The relayed message must carry a source address.");
            string sourceVirtualHost = GetVirtualHost(sourceAddress);
            string inputVirtualHost = GetVirtualHost(context.Advanced().ReceiveContext.InputAddress);
            bool forward = sourceVirtualHost == inputVirtualHost;
            decisions.Enqueue(new RelayDecision(
                context.MessageId,
                sourceVirtualHost,
                inputVirtualHost,
                forward));

            if (!forward)
                return Task.CompletedTask;

            return otherHost.PublishAsync(
                context.Message,
                new CopyContextPipe(context.Advanced()),
                context.CancellationToken);
        }
    }

    private sealed class RealConsumer(ConcurrentQueue<Delivery> deliveries) : IConsumer<IsolationMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IsolationMessage> context)
        {
            Uri sourceAddress = context.SourceAddress
                ?? throw new InvalidOperationException("The delivered message must carry its original source address.");
            deliveries.Enqueue(new Delivery(
                context.MessageId,
                context.Message.Token,
                sourceAddress,
                context.Advanced().ReceiveContext.InputAddress));

            return Task.CompletedTask;
        }
    }
}
