using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqLifecycleTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0463", "restart-reacquires-connection-without-message-loss")]
    public async Task Restart_ReacquiresConnectionWithoutMessageLossAsync(string flavor)
    {
        const int messageCount = 12;
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "restart-retention");
        string queueName = fixture.Name("input");
        Guid[] expected = [.. Enumerable.Range(0, messageCount).Select(_ => Guid.NewGuid())];
        var firstEntered = NewObservation<bool>();
        var releaseFirst = NewObservation<bool>();
        var allDelivered = NewObservation<bool>();
        var duplicate = NewObservation<Guid>();
        var delivered = new ConcurrentDictionary<Guid, byte>();
        IBusControl receiver = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = 1;
                endpoint.ConcurrentMessageLimit = 1;
                endpoint.Handler<RetainedMessage>(async context =>
                {
                    if (context.Message.FlowId == expected[0])
                    {
                        firstEntered.TrySetResult(true);
                        await releaseFirst.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                    }

                    if (!delivered.TryAdd(context.Message.FlowId, 0))
                        duplicate.TrySetResult(context.Message.FlowId);
                    if (delivered.Count == messageCount)
                        allDelivered.TrySetResult(true);
                });
            });
        });
        IBusControl publisher = Bus.Factory.CreateUsingActiveMq(configurator => fixture.ConfigureHost(configurator));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool receiverStarted = false;
        bool publisherStarted = false;

        try
        {
            await publisher.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            publisherStarted = true;
            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            ISendEndpoint input = await publisher.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(new RetainedMessage(expected[0]), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await firstEntered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Task stop = receiver.StopAsync(CancellationToken.None);
            releaseFirst.TrySetResult(true);
            await stop.WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            receiverStarted = false;
            Assert.Equal([expected[0]], delivered.Keys);

            await Task.WhenAll(expected[1..].Select(flowId => input.SendAsync(new RetainedMessage(flowId), cancellationToken)))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Single(delivered);

            await receiver.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            receiverStarted = true;
            Task finished = await Task.WhenAny(allDelivered.Task, duplicate.Task)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            if (ReferenceEquals(finished, duplicate.Task))
                throw new InvalidDataException($"Message '{await duplicate.Task}' was delivered more than once.");
            await allDelivered.Task;

            Assert.Equal(expected.Order(), delivered.Keys.Order());
            Assert.Equal(messageCount, delivered.Count);
        }
        finally
        {
            releaseFirst.TrySetResult(true);
            if (receiverStarted)
                await receiver.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (publisherStarted)
                await publisher.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0464", "start-stop-start-is-idempotent-and-preserves-request-routing")]
    public async Task StartStopStart_IsIdempotentAndLeavesNoRunResourcesAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "start-stop");
        string queueName = fixture.Name("service");
        var handled = new ConcurrentQueue<Guid>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint => endpoint.Handler<CycleRequest>(context =>
            {
                handled.Enqueue(context.Message.FlowId);
                return context.RespondAsync(new CycleResponse(context.Message.FlowId));
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            Guid first = await ExecuteCycleAsync();
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Guid second = await ExecuteCycleAsync();

            Assert.NotEqual(first, second);
            Assert.Equal([first, second], handled.ToArray());
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }

        async Task<Guid> ExecuteCycleAsync()
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid flowId = Guid.NewGuid();
            IRequestClient<CycleRequest> client = bus.CreateRequestClient<CycleRequest>(
                new Uri($"queue:{queueName}"),
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            Response<CycleResponse> response = await client.GetResponseAsync<CycleResponse>(
                    new CycleRequest(flowId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(flowId, response.Message.FlowId);
            return flowId;
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record RetainedMessage(Guid FlowId);
    public sealed record CycleRequest(Guid FlowId);
    public sealed record CycleResponse(Guid FlowId);
}

public sealed class ActiveMqRecoveryTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0446", "send-endpoint-cache-turnover-preserves-delivery-across-protocols")]
    public async Task SendEndpointCacheTurnover_PreservesDeliveryAcrossProtocolsAsync(string flavor)
    {
        const int cacheCapacity = 1000;
        const int churnCount = cacheCapacity + 4;
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "cache-turnover");
        string targetQueue = fixture.Name("target");
        Guid before = Guid.NewGuid();
        Guid after = Guid.NewGuid();
        var delivered = NewObservation<Guid[]>();
        var identities = new ConcurrentQueue<Guid>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(targetQueue, endpoint => endpoint.Handler<TurnoverMessage>(context =>
            {
                identities.Enqueue(context.Message.FlowId);
                if (identities.Count == 2)
                    delivered.TrySetResult(identities.ToArray());
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint target = await bus.GetSendEndpointAsync(new Uri($"queue:{targetQueue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await target.SendAsync(new TurnoverMessage(before), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            for (int index = 0; index < churnCount; index++)
            {
                ISendEndpoint churn = await bus.GetSendEndpointAsync(new Uri($"queue:{fixture.Name($"churn-{index}")}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
                await churn.SendAsync(new ChurnMessage(index), cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            ISendEndpoint reacquired = await bus.GetSendEndpointAsync(new Uri($"queue:{targetQueue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await reacquired.SendAsync(new TurnoverMessage(after), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal([before, after], await delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record TurnoverMessage(Guid FlowId);
    public sealed record ChurnMessage(int Sequence);
}
