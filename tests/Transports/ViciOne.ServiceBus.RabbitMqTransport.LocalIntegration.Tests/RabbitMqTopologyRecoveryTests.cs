using ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

public sealed class RabbitMqTopologyRecoveryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-TOPOLOGY-RECOVERY", "external-queue-deletion-redeclares-topology")]
    public async Task ExternalQueueDeletion_InvalidatesCachedTopologyAndRedeclaresTheCompleteRouteAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("topologyrecovery");
        string queue = fixture.Name("input");
        var firstReceived = NewObservation<string>();
        var secondReceived = NewObservation<string>();
        var firstEntries = 0;
        var secondEntries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<RecoveryMessage>(context =>
                {
                    if (context.Message.Value == "before")
                    {
                        if (Interlocked.Increment(ref firstEntries) == 1)
                            firstReceived.TrySetResult(context.Message.Value);
                        else
                            firstReceived.TrySetException(new InvalidDataException("The first generation was delivered more than once."));
                    }
                    else if (context.Message.Value == "after")
                    {
                        if (Interlocked.Increment(ref secondEntries) == 1)
                            secondReceived.TrySetResult(context.Message.Value);
                        else
                            secondReceived.TrySetException(new InvalidDataException("The recovered generation was delivered more than once."));
                    }

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
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new RecoveryMessage("before"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal("before", await firstReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await fixture.DeleteQueueAsync(queue, cancellationToken);
            await fixture.WaitUntilQueueBindingExistsAsync(queue, queue, cancellationToken);

            RabbitMqBroker.QueueState recovered = await fixture.QueueAsync(queue, cancellationToken);
            IReadOnlyList<RabbitMqBroker.BindingState> bindings = await fixture.QueueBindingsAsync(queue, cancellationToken);
            Assert.True(recovered.Exists);
            Assert.True(recovered.Durable);
            Assert.Contains(bindings, binding => binding.Source == queue && binding.Destination == queue);

            await endpoint.SendAsync(new RecoveryMessage("after"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal("after", await secondReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.Equal((1, 1), (firstEntries, secondEntries));
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record RecoveryMessage(string Value);
}
