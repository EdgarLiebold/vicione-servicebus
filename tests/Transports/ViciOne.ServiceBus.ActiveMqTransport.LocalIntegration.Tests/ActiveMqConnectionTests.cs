using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

public sealed class ActiveMqConnectionTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0379", "explicit-classic-endpoint-reaches-ready")]
    public async Task OpenWireEndpoint_ReachesReady(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "ready");
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(fixture.ConfigureHost);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            Assert.Equal(BusHealthStatus.Healthy, bus.CheckHealth().Status);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("OBL-R0-BRK-0387", "explicit-configuration-is-healthy-and-delivers")]
    public async Task ValidExplicitConfiguration_ReachesReadyAndHealthy(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "configured");
        string queueName = fixture.Name("input");
        Guid expected = Guid.NewGuid();
        var received = NewObservation<Guid>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<ReadyMessage>(context =>
                {
                    received.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Assert.Equal(BusHealthStatus.Healthy, bus.CheckHealth().Status);

            ISendEndpoint input = await bus.GetSendEndpoint(new Uri($"queue:{queueName}"))
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.Send(new ReadyMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record ReadyMessage(Guid CorrelationId);
}
