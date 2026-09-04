using System.Collections.Concurrent;
using ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

public sealed class RabbitMqMessageFlowTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-FLOW", "send-preserves-envelope-and-provider-properties-exactly-once")]
    public async Task Send_PreservesEnvelopeAndProviderPropertiesExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("flow");
        string queue = fixture.Name("input");
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        Guid requestId = NewId.NextGuid();
        var received = NewObservation<ConsumeContext<FlowMessage>>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.PrefetchCount = 4;
                endpoint.Handler<FlowMessage>(context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        received.TrySetException(new InvalidDataException("The sent message was delivered more than once."));
                    else
                        received.TrySetResult(context);
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
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await input.SendAsync(
                    new FlowMessage("exact-payload"),
                    context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.ConversationId = conversationId;
                        context.RequestId = requestId;
                        context.Headers.Set("native-marker", "marker-value");
                        RabbitMqSendContext provider = context.GetPayload<RabbitMqSendContext>();
                        provider.RoutingKey = "exact-route";
                        provider.BasicProperties.Priority = 7;
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            ConsumeContext<FlowMessage> actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            RabbitMqBasicConsumeContext provider = actual.GetPayload<RabbitMqBasicConsumeContext>();
            Assert.Equal("exact-payload", actual.Message.Value);
            Assert.Equal(messageId, actual.MessageId);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(conversationId, actual.ConversationId);
            Assert.Equal(requestId, actual.RequestId);
            Assert.Equal("marker-value", actual.Headers.Get<string>("native-marker"));
            Assert.Equal(queue, provider.Exchange);
            Assert.Equal("exact-route", provider.RoutingKey);
            Assert.Equal((byte)7, provider.Properties.Priority);
            Assert.False(actual.Advanced().ReceiveContext.Redelivered);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            RabbitMqBroker.QueueState state = await fixture.QueueAsync(queue, cancellationToken);
            Assert.True(state.Exists);
            Assert.True(state.Durable);
            Assert.False(state.AutoDelete);
            Assert.False(state.Exclusive);
            Assert.Equal(0, state.Consumers);
            Assert.Equal(0, state.Messages);
            Assert.Equal(1, entries);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-PUBLISH", "hierarchy-bindings-and-exact-delivery")]
    public async Task PublishHierarchy_DeclaresProviderBindingsAndDeliversExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("hierarchy");
        string queue = fixture.Name("input");
        string baseExchange = fixture.Name("base");
        string derivedExchange = fixture.Name("derived");
        Guid expected = NewId.NextGuid();
        var received = NewObservation<Guid>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<IBaseContract>().SetEntityName(baseExchange);
            configurator.MessageTopology.GetMessageTopology<IDerivedContract>().SetEntityName(derivedExchange);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<IBaseContract>(context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        received.TrySetException(new InvalidDataException("The hierarchy publish was delivered more than once."));
                    else
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
            await bus.PublishAsync<IDerivedContract>(new { CorrelationId = expected }, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            RabbitMqBroker.ExchangeState baseState = await fixture.ExchangeAsync(baseExchange, cancellationToken);
            RabbitMqBroker.ExchangeState derivedState = await fixture.ExchangeAsync(derivedExchange, cancellationToken);
            Assert.True(baseState.Exists);
            Assert.True(derivedState.Exists);
            Assert.Equal("fanout", baseState.Type);
            Assert.Equal("fanout", derivedState.Type);
            IReadOnlyList<RabbitMqBroker.BindingState> bindings = await fixture.BindingsAsync(cancellationToken);
            Assert.Contains(
                bindings,
                binding => binding.Source == derivedExchange
                           && binding.Destination == baseExchange
                           && binding.DestinationType == "exchange");
            Assert.Contains(
                bindings,
                binding => binding.Source == baseExchange
                           && binding.Destination == queue
                           && binding.DestinationType == "exchange");

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            RabbitMqBroker.QueueState terminal = await fixture.QueueAsync(queue, cancellationToken);
            Assert.Equal(0, terminal.Messages);
            Assert.Equal(1, entries);
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

    private sealed record FlowMessage(string Value);

}

public interface IBaseContract
{
    Guid CorrelationId { get; }
}

public interface IDerivedContract : IBaseContract;
