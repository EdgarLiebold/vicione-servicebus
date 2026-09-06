using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

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
