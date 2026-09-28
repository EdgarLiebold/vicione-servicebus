using System.Collections.Concurrent;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqProducerIsolationTests
{
    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "native-send-settings-and-destinations-remain-independent")]
    public async Task SequentialSends_PreservePriorityDurabilityAndDestinationAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "producer-isolation");
        string firstQueue = fixture.Name("first");
        string secondQueue = fixture.Name("second");
        ProducerMessage[] messages =
        [
            new(Guid.NewGuid(), "first nonpersistent message"),
            new(Guid.NewGuid(), "other destination"),
            new(Guid.NewGuid(), "reused first destination"),
        ];
        var observations = new ConcurrentQueue<NativeObservation>();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            ConfigureReceive(firstQueue);
            ConfigureReceive(secondQueue);

            void ConfigureReceive(string queue)
            {
                configurator.ReceiveEndpoint(queue, endpoint =>
                {
                    endpoint.Durable = false;
                    endpoint.AutoDelete = true;
                    endpoint.Handler<ProducerMessage>(context =>
                    {
                        try
                        {
                            var transport = Assert.IsType<ActiveMqReceiveContext>(context.Advanced().ReceiveContext);
                            IMessage native = transport.TransportMessage;
                            observations.Enqueue(new NativeObservation(queue, context.Message, context.MessageId,
                                native.NMSPriority, native.NMSDeliveryMode));
                            if (observations.Count == messages.Length)
                                received.TrySetResult();
                            return Task.CompletedTask;
                        }
                        catch (Exception exception)
                        {
                            received.TrySetException(exception);
                            return Task.FromException(exception);
                        }
                    });
                });
            }
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;
        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint first = await bus.GetSendEndpointAsync(new Uri($"queue:{firstQueue}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ISendEndpoint second = await bus.GetSendEndpointAsync(new Uri($"queue:{secondQueue}"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            await first.SendAsync(messages[0], context =>
            {
                context.MessageId = messages[0].Id;
                context.Durable = false;
                context.GetPayload<ActiveMqSendContext>().Priority = MsgPriority.VeryHigh;
            }, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await second.SendAsync(messages[1], context =>
            {
                context.MessageId = messages[1].Id;
                context.Durable = true;
            }, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await first.SendAsync(messages[2], context =>
            {
                context.MessageId = messages[2].Id;
                context.Durable = true;
            }, cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal(messages.Length, observations.Count);
            NativeObservation nonpersistent = Assert.Single(observations, value => value.Message.Id == messages[0].Id);
            Assert.Equal(firstQueue, nonpersistent.Queue);
            Assert.Equal(messages[0], nonpersistent.Message);
            Assert.Equal(messages[0].Id, nonpersistent.MessageId);
            Assert.Equal(MsgPriority.VeryHigh, nonpersistent.Priority);
            Assert.Equal(MsgDeliveryMode.NonPersistent, nonpersistent.DeliveryMode);

            NativeObservation other = Assert.Single(observations, value => value.Message.Id == messages[1].Id);
            Assert.Equal(secondQueue, other.Queue);
            Assert.Equal(messages[1], other.Message);
            Assert.Equal(messages[1].Id, other.MessageId);
            Assert.Equal(MsgPriority.Normal, other.Priority);
            Assert.Equal(MsgDeliveryMode.Persistent, other.DeliveryMode);

            NativeObservation reused = Assert.Single(observations, value => value.Message.Id == messages[2].Id);
            Assert.Equal(firstQueue, reused.Queue);
            Assert.Equal(messages[2], reused.Message);
            Assert.Equal(messages[2].Id, reused.MessageId);
            Assert.Equal(MsgPriority.Normal, reused.Priority);
            Assert.Equal(MsgDeliveryMode.Persistent, reused.DeliveryMode);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private sealed record ProducerMessage(Guid Id, string Text);

    private sealed record NativeObservation(string Queue, ProducerMessage Message, Guid? MessageId,
        MsgPriority Priority, MsgDeliveryMode DeliveryMode);
}
