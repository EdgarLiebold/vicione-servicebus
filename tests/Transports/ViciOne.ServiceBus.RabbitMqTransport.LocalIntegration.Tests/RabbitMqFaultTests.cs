using System.Runtime.Serialization;
using ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

public sealed class RabbitMqFaultTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FAULT", "retry-fault-and-error-move-are-terminal-and-exact")]
    public async Task RetryExhaustion_MovesOneEnvelopeAndPublishesOneCorrelatedFaultAsync()
    {
        const string failureMessage = "intentional RabbitMQ consumer failure";
        using RabbitMqBroker fixture = RabbitMqBroker.Create("fault");
        string queue = fixture.Name("input");
        string errorQueue = queue + "_error";
        string faultQueue = fixture.Name("faults");
        Guid correlationId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        var moved = NewObservation<ConsumeContext<FailureMessage>>();
        var faulted = NewObservation<ConsumeContext<Fault<FailureMessage>>>();
        int sourceEntries = 0;
        int movedEntries = 0;
        int faultEntries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.UseMessageRetry(retry => retry.Immediate(2));
                endpoint.Handler<FailureMessage>(_ =>
                {
                    Interlocked.Increment(ref sourceEntries);
                    throw new SerializationException(failureMessage);
                });
            });
            configurator.ReceiveEndpoint(errorQueue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<FailureMessage>(context =>
                {
                    if (Interlocked.Increment(ref movedEntries) != 1)
                        moved.TrySetException(new InvalidDataException("The failed envelope was moved more than once."));
                    else
                        moved.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
            configurator.ReceiveEndpoint(faultQueue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Handler<Fault<FailureMessage>>(context =>
                {
                    if (Interlocked.Increment(ref faultEntries) != 1)
                        faulted.TrySetException(new InvalidDataException("The terminal fault was published more than once."));
                    else
                        faulted.TrySetResult(context);
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
            await endpoint.SendAsync(
                    new FailureMessage("must-move"),
                    context =>
                    {
                        context.CorrelationId = correlationId;
                        context.ConversationId = conversationId;
                        context.FaultAddress = new Uri($"queue:{faultQueue}");
                        context.Headers.Set("fault-marker", "exact-marker");
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<FailureMessage> movedContext = await moved.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<Fault<FailureMessage>> faultContext = await faulted.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal("must-move", movedContext.Message.Value);
            Assert.Equal(correlationId, movedContext.CorrelationId);
            Assert.Equal(conversationId, movedContext.ConversationId);
            Assert.Equal("exact-marker", movedContext.Headers.Get<string>("fault-marker"));
            Assert.Equal(failureMessage,
                movedContext.Advanced().ReceiveContext.TransportHeaders.Get(MessageHeaders.FaultMessage, default(string)));
            Assert.Equal("fault",
                movedContext.Advanced().ReceiveContext.TransportHeaders.Get(MessageHeaders.Reason, default(string)));
            Assert.Equal(correlationId, faultContext.CorrelationId);
            Assert.Equal(conversationId, faultContext.ConversationId);
            Assert.Equal("must-move", faultContext.Message.Message.Value);
            Assert.Equal(failureMessage, Assert.Single(faultContext.Message.Exceptions).Message);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal((3, 1, 1), (sourceEntries, movedEntries, faultEntries));
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
            Assert.Equal(0, (await fixture.QueueAsync(errorQueue, cancellationToken)).Messages);
            Assert.Equal(0, (await fixture.QueueAsync(faultQueue, cancellationToken)).Messages);
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

    private sealed record FailureMessage(string Value);
}
