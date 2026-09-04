using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-TOPOLOGY", "alternate-dead-letter-priority-and-expiration-are-broker-owned")]
    public async Task BrokerTopology_DeclaresAlternateDeadLetterPriorityAndExpirationContractsAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("policies");
        string primary = fixture.Name("primary");
        string alternateExchange = fixture.Name("alternate");
        string alternateQueue = fixture.Name("alternate-queue");
        string inputQueue = fixture.Name("input");
        string deadLetterQueue = fixture.Name("dead-letter");
        Guid expected = NewId.NextGuid();
        var alternateReceived = NewObservation<Guid>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<AlternateMessage>().SetEntityName(primary);
            configurator.PublishTopology.GetMessageTopology<AlternateMessage>()
                .BindAlternateExchangeQueue(alternateExchange);
            configurator.ReceiveEndpoint(alternateQueue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Bind(alternateExchange);
                endpoint.Handler<AlternateMessage>(context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        alternateReceived.TrySetException(new InvalidDataException("Alternate routing duplicated the message."));
                    else
                        alternateReceived.TrySetResult(context.Message.CorrelationId);
                    return Task.CompletedTask;
                });
            });
            configurator.ReceiveEndpoint(inputQueue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.QueueExpiration = TimeSpan.FromMinutes(90);
                endpoint.SetQueueArgument("x-max-priority", 10);
                endpoint.BindDeadLetterQueue(deadLetterQueue);
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Assert.True(bus.Topology.TryGetPublishAddress<AlternateMessage>(out Uri? publishAddress));
            Assert.Contains(
                $"alternateexchange={Uri.EscapeDataString(alternateExchange)}",
                publishAddress.Query,
                StringComparison.OrdinalIgnoreCase);

            RabbitMqBroker.QueueState input = await fixture.QueueAsync(inputQueue, cancellationToken);
            Assert.True(input.Exists);
            Assert.True(input.Durable);
            Assert.Equal("10", input.Arguments["x-max-priority"]);
            Assert.Equal("5400000", input.Arguments["x-expires"]);
            Assert.Equal(deadLetterQueue, input.Arguments["x-dead-letter-exchange"]);
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(alternateQueue, cancellationToken));
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(deadLetterQueue, cancellationToken));
            await fixture.AssertExchangeExistsAsync(deadLetterQueue, cancellationToken);

            await bus.PublishAsync(new AlternateMessage(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, await alternateReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            RabbitMqBroker.ExchangeState primaryState = await fixture.ExchangeAsync(primary, cancellationToken);
            RabbitMqBroker.ExchangeState alternateState = await fixture.ExchangeAsync(alternateExchange, cancellationToken);
            Assert.True(primaryState.Exists);
            Assert.Equal(alternateExchange, primaryState.Arguments["alternate-exchange"]);
            Assert.True(alternateState.Exists);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
            Assert.Equal(0, (await fixture.QueueAsync(alternateQueue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Theory]
    [InlineData(ExchangeType.Direct, "route.alpha", "route.beta")]
    [InlineData(ExchangeType.Topic, "region.eu.alpha", "region.us.beta")]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-ROUTING", "direct-and-topic-bindings-route-without-cross-delivery")]
    public async Task RoutingKeys_DeclareExactProviderBindingsAndNeverCrossDeliverAsync(
        string exchangeType,
        string firstKey,
        string secondKey)
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("routing");
        string exchange = fixture.Name(exchangeType);
        string firstQueue = fixture.Name("first");
        string secondQueue = fixture.Name("second");
        var firstReceived = NewObservation<string>();
        var secondReceived = NewObservation<string>();
        int firstEntries = 0;
        int secondEntries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Message<RoutingMessage>(topology => topology.SetEntityName(exchange));
            configurator.Publish<RoutingMessage>(topology => topology.ExchangeType = exchangeType);
            configurator.Send<RoutingMessage>(topology =>
                topology.UseRoutingKeyFormatter(context => context.Message.RoutingKey));
            ConfigureRoute(configurator, firstQueue, firstKey, exchangeType, firstReceived, () =>
                Interlocked.Increment(ref firstEntries));
            ConfigureRoute(configurator, secondQueue, secondKey, exchangeType, secondReceived, () =>
                Interlocked.Increment(ref secondEntries));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            RabbitMqBroker.ExchangeState state = await fixture.ExchangeAsync(exchange, cancellationToken);
            Assert.Equal(exchangeType, state.Type);
            IReadOnlyList<RabbitMqBroker.BindingState> bindings = await fixture.BindingsAsync(cancellationToken);
            Assert.Contains(bindings, binding =>
                binding.Source == exchange && binding.Destination == firstQueue && binding.RoutingKey == firstKey);
            Assert.Contains(bindings, binding =>
                binding.Source == exchange && binding.Destination == secondQueue && binding.RoutingKey == secondKey);

            await bus.PublishAsync(new RoutingMessage(firstKey, "first"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(new RoutingMessage(secondKey, "second"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal("first", await firstReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.Equal("second", await secondReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal((1, 1), (firstEntries, secondEntries));
            Assert.Equal(0, (await fixture.QueueAsync(firstQueue, cancellationToken)).Messages);
            Assert.Equal(0, (await fixture.QueueAsync(secondQueue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static void ConfigureRoute(
        IRabbitMqBusFactoryConfigurator configurator,
        string queue,
        string routingKey,
        string exchangeType,
        TaskCompletionSource<string> received,
        Func<int> increment)
    {
        configurator.ReceiveEndpoint(queue, endpoint =>
        {
            endpoint.Durable = true;
            endpoint.AutoDelete = false;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.Bind<RoutingMessage>(binding =>
            {
                binding.ExchangeType = exchangeType;
                binding.RoutingKey = routingKey;
            });
            endpoint.Handler<RoutingMessage>(context =>
            {
                if (increment() != 1)
                    received.TrySetException(new InvalidDataException($"Queue {queue} received more than one message."));
                else
                    received.TrySetResult(context.Message.Value);
                return Task.CompletedTask;
            });
        });
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record AlternateMessage(Guid CorrelationId);

    private sealed record RoutingMessage(string RoutingKey, string Value);
}
