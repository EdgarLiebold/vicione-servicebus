namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

using Infrastructure;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class RabbitMqQueueRedeliveryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-QUEUE-REDELIVERY", "classic-and-quorum-ttl-dlx-roundtrip")]
    public async Task QueueRedelivery_PredeclaresFiniteTopologyAndReturnsThroughTheOriginalEndpoint(bool quorum)
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create(quorum ? "redeliveryquorum" : "redelivery");
        string queue = fixture.Name("input");
        string exchange = fixture.Name("message");
        const string originalRoutingKey = "region.eu.orders";
        const int intervalMilliseconds = 250;
        string delayExchange = queue + ".redelivery";
        string returnExchange = delayExchange + ".return";
        string delayQueue = delayExchange + $".{intervalMilliseconds}";
        var received = NewObservation<RedeliveryObservation>();
        var attempts = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.Message<RetryMessage>(topology => topology.SetEntityName(exchange));
            configurator.Publish<RetryMessage>(topology => topology.ExchangeType = ExchangeType.Direct);
            configurator.Send<RetryMessage>(topology =>
                topology.UseRoutingKeyFormatter(_ => originalRoutingKey));
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                if (quorum)
                    endpoint.SetQuorumQueue(1);
                endpoint.ConfigureConsumeTopology = false;
                endpoint.Bind<RetryMessage>(binding =>
                {
                    binding.ExchangeType = ExchangeType.Direct;
                    binding.RoutingKey = originalRoutingKey;
                });
                endpoint.UseQueueRedelivery(TimeSpan.FromMilliseconds(intervalMilliseconds));
                endpoint.Handler<RetryMessage>(context =>
                {
                    int attempt = Interlocked.Increment(ref attempts);
                    if (attempt == 1)
                        throw new InvalidOperationException("transient test failure");

                    received.TrySetResult(new RedeliveryObservation(
                        context.Message.Value,
                        context.GetRedeliveryCount(),
                        context.Headers.Get<string>(RabbitMqHeaders.RedeliveryRoutingKey)));
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

            RabbitMqBroker.ExchangeState delayExchangeState = await fixture.Exchange(delayExchange, cancellationToken);
            RabbitMqBroker.ExchangeState returnExchangeState = await fixture.Exchange(returnExchange, cancellationToken);
            RabbitMqBroker.QueueState delayQueueState = await fixture.Queue(delayQueue, cancellationToken);
            Assert.Equal((true, ExchangeType.Direct, true, false),
                (delayExchangeState.Exists, delayExchangeState.Type, delayExchangeState.Durable, delayExchangeState.AutoDelete));
            Assert.Equal((true, ExchangeType.Direct, true, false),
                (returnExchangeState.Exists, returnExchangeState.Type, returnExchangeState.Durable, returnExchangeState.AutoDelete));
            Assert.Equal((true, true, false, false),
                (delayQueueState.Exists, delayQueueState.Durable, delayQueueState.AutoDelete, delayQueueState.Exclusive));
            Assert.Equal(intervalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                delayQueueState.Arguments["x-message-ttl"]);
            Assert.Equal(returnExchange, delayQueueState.Arguments["x-dead-letter-exchange"]);
            Assert.Equal(queue, delayQueueState.Arguments["x-dead-letter-routing-key"]);
            if (quorum)
            {
                Assert.Equal("quorum", delayQueueState.Arguments["x-queue-type"]);
                Assert.Equal("1", delayQueueState.Arguments["x-quorum-initial-group-size"]);
            }
            else
                Assert.Equal("classic", delayQueueState.Arguments["x-queue-type"]);

            IReadOnlyList<RabbitMqBroker.BindingState> delayBindings = await fixture.QueueBindings(delayQueue, cancellationToken);
            IReadOnlyList<RabbitMqBroker.BindingState> sourceBindings = await fixture.QueueBindings(queue, cancellationToken);
            Assert.Contains(delayBindings, binding =>
                binding.Source == delayExchange
                && binding.Destination == delayQueue
                && binding.RoutingKey == intervalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.Contains(sourceBindings, binding =>
                binding.Source == returnExchange
                && binding.Destination == queue
                && binding.RoutingKey == queue);

            await bus.Publish(new RetryMessage("redelivered"), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            RedeliveryObservation observation = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal("redelivered", observation.Value);
            Assert.Equal(1, observation.RedeliveryCount);
            Assert.Equal(originalRoutingKey, observation.OriginalRoutingKey);
            Assert.Equal(2, attempts);
            Assert.Equal(0, (await fixture.Queue(queue, cancellationToken)).Messages);
            Assert.Equal(0, (await fixture.Queue(delayQueue, cancellationToken)).Messages);
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

    private sealed record RetryMessage(string Value);

    private sealed record RedeliveryObservation(string Value, int RedeliveryCount, string? OriginalRoutingKey);
}
