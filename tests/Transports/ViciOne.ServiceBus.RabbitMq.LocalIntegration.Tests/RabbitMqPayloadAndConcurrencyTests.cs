using System.Collections.Concurrent;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqPayloadAndConcurrencyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-PAYLOAD", "binary-payload-round-trips-byte-for-byte")]
    public async Task BinaryPayload_RoundTripsByteForByteExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("binary");
        string queue = fixture.Name("input");
        byte[] expected = Enumerable.Range(0, 512).Select(index => checked((byte)(index % 251))).ToArray();
        var received = NewObservation<BinaryPayload>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint => endpoint.Handler<BinaryPayload>(context =>
            {
                if (Interlocked.Increment(ref entries) != 1)
                    received.TrySetException(new InvalidDataException("The binary payload was delivered more than once."));
                else
                    received.TrySetResult(context.Message);
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(new BinaryPayload(expected), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            BinaryPayload actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(expected, actual.Contents);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-PAYLOAD", "raw-json-preserves-payload-header-and-envelope-identities")]
    public async Task RawJson_PreservesPayloadHeaderAndEnvelopeIdentitiesExactlyOnceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("rawjson");
        string queue = fixture.Name("input");
        Guid expected = NewId.NextGuid();
        var received = NewObservation<ConsumeContext<RawPayload>>();
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.UseRawJsonSerializer(RawSerializerOptions.All);
            configurator.ReceiveEndpoint(queue, endpoint => endpoint.Handler<RawPayload>(context =>
            {
                if (Interlocked.Increment(ref entries) != 1)
                    received.TrySetException(new InvalidDataException("The raw JSON payload was delivered more than once."));
                else
                    received.TrySetResult(context);
                return Task.CompletedTask;
            }));
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri($"queue:{queue}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await endpoint.SendAsync(
                    new RawPayload(expected, "raw-value"),
                    context => context.Headers.Set("raw-marker", "exact-marker"),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<RawPayload> actual = await received.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, actual.Message.CorrelationId);
            Assert.Equal("raw-value", actual.Message.Value);
            Assert.Equal("exact-marker", actual.Headers.Get<string>("raw-marker"));
            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType, actual.Advanced().ReceiveContext.ContentType);
            Assert.NotNull(actual.MessageId);
            Assert.NotNull(actual.ConversationId);
            Assert.NotNull(actual.CorrelationId);
            Assert.NotNull(actual.DestinationAddress);
            Assert.NotNull(actual.Host);
            Assert.Single(actual.Advanced().SupportedMessageTypes);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(1, entries);
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-CONCURRENCY", "one-hundred-deliveries-obey-exact-consumer-limit")]
    public async Task ConsumerConcurrency_ProcessesOneHundredUniqueMessagesWithAnExactMaximumOfTwoAsync()
    {
        const int expectedCount = 100;
        using RabbitMqBroker fixture = RabbitMqBroker.Create("concurrency");
        string queue = fixture.Name("input");
        var release = NewObservation();
        var reachedLimit = NewObservation();
        var completed = NewObservation();
        var identities = new ConcurrentDictionary<int, byte>();
        int active = 0;
        int maximum = 0;
        int consumed = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.PrefetchCount = 8;
                endpoint.ConcurrentMessageLimit = 2;
                endpoint.Handler<ConcurrentPayload>(async context =>
                {
                    Assert.True(identities.TryAdd(context.Message.Identity, 0),
                        $"Message {context.Message.Identity} was delivered more than once.");
                    int current = Interlocked.Increment(ref active);
                    UpdateMaximum(ref maximum, current);
                    if (current == 2)
                        reachedLimit.TrySetResult();
                    await release.Task.WaitAsync(fixture.OperationTimeout, context.CancellationToken);
                    Interlocked.Decrement(ref active);
                    if (Interlocked.Increment(ref consumed) == expectedCount)
                        completed.TrySetResult();
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
            Task[] sends = Enumerable.Range(0, expectedCount)
                .Select(identity => endpoint.SendAsync(new ConcurrentPayload(identity), cancellationToken))
                .ToArray();
            await Task.WhenAll(sends).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await reachedLimit.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(2, Volatile.Read(ref active));
            release.TrySetResult();
            await completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            Assert.Equal(expectedCount, consumed);
            Assert.Equal(expectedCount, identities.Count);
            Assert.Equal(2, maximum);
            Assert.Equal(0, (await fixture.QueueAsync(queue, cancellationToken)).Messages);
        }
        finally
        {
            release.TrySetResult();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static void UpdateMaximum(ref int maximum, int current)
    {
        int observed;
        while (current > (observed = Volatile.Read(ref maximum))
               && Interlocked.CompareExchange(ref maximum, current, observed) != observed)
        {
        }
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record ConcurrentPayload(int Identity);

    private sealed record BinaryPayload(byte[] Contents);

    private sealed record RawPayload(Guid CorrelationId, string Value);
}
