using System.Collections.Concurrent;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubInteropAndContextTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0156", "raw-sdk-event-is-consumed-and-published-to-bus-with-eventhub-source-and-initiator")]
    public async Task RawSdkEvent_IsConsumedAndPublishesOntoTheBusWithTransportConversationMetadataAsync()
    {
        const string eventHubName = "raw-eh";
        var state = new RawInteropState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("raw-interop");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<RawBusPingConsumer>();
                configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<RawInteropConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("raw-interop");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<RawInteropConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            Guid correlationId = NewId.NextGuid();
            var source = new RawInteropMessage(state.RunId, "foreign-producer");
            var sendContext = new MessageSendContext<IRawInteropMessage>(source)
            {
                Serializer = ServiceBusMetadataJson.MessageSerializer,
                CorrelationId = correlationId,
            };
            var eventData = new EventData(ServiceBusMetadataJson.MessageSerializer.GetMessageBody(sendContext).ToArray())
            {
                ContentType = SystemTextJsonMessageSerializer.JsonContentType.MediaType,
            };
            await using EventHubProducerClient producer = fixture.CreateRawProducer(eventHubName);

            await producer.SendAsync([eventData], cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<IRawInteropMessage> received = await state.Received.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<RawBusPing> ping = await state.Ping.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(source.RunId, received.Message.RunId);
            Assert.Equal(source.Text, received.Message.Text);
            Assert.Equal(correlationId, received.CorrelationId);
            Assert.Equal(received.CorrelationId, ping.InitiatorId);
            Assert.Equal(
                new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/{eventHubName}/{EventHubLocalFixture.ConsumerGroup}"),
                ping.SourceAddress);
            Assert.Equal(state.RunId, ping.Message.RunId);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0157", "raw-sdk-envelope-with-default-send-context-serializer-deserializes-to-contract")]
    public async Task RawSdkEnvelope_UsesTheDefaultMessageSerializerAndDeserializesToTheContractAsync()
    {
        const string eventHubName = "raw-eh";
        var state = new DefaultSerializerState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("default-serializer");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<DefaultSerializerConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("default");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<DefaultSerializerConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            var source = new DefaultSerializerMessage(state.RunId, "default-contract");
            var sendContext = new MessageSendContext<IDefaultSerializerMessage>(source);
            Assert.Null(sendContext.Serializer);
            var eventData = new EventData(ServiceBusMetadataJson.MessageSerializer.GetMessageBody(sendContext).ToArray());
            await using EventHubProducerClient producer = fixture.CreateRawProducer(eventHubName);

            await producer.SendAsync([eventData], cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            IDefaultSerializerMessage actual = await state.Received.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(source.RunId, actual.RunId);
            Assert.Equal(source.Text, actual.Text);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0158", "consume-context-exposes-the-eventhub-provider-payload")]
    public async Task RiderDelivery_ExposesTheEventHubConsumeContextPayloadAsync()
    {
        ContextProbeObservation observation = await RunContextProbeAsync("payload", messageCount: 1);

        Assert.True(observation.PayloadPresent);
        Assert.NotNull(observation.PartitionId);
        Assert.NotEmpty(observation.OffsetString);
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0259", "provider-context-reports-partition-offset-sequence-key-enqueued-time-and-properties")]
    public async Task EventHubConsumeContext_ReportsProviderAssignedFieldsForTwoSamePartitionEventsAsync()
    {
        ContextProbeObservation observation = await RunContextProbeAsync("provider-context", messageCount: 2);
        ContextSnapshot[] actual = observation.Snapshots.OrderBy(snapshot => snapshot.Index).ToArray();

        Assert.Equal(2, actual.Length);
        Assert.All(actual, snapshot =>
        {
            Assert.True(snapshot.PayloadPresent);
            Assert.Contains(snapshot.PartitionId, new[] { "0", "1", "2", "3" });
            Assert.NotEmpty(snapshot.OffsetString);
            Assert.Equal(observation.PartitionKey, snapshot.PartitionKey);
            Assert.NotEqual(default, snapshot.EnqueuedTime);
            Assert.Equal(observation.RunId.ToString("N"), snapshot.Properties["run-id"]?.ToString());
            Assert.NotNull(snapshot.SystemProperties);
        });
        Assert.Equal(actual[0].PartitionId, actual[1].PartitionId);
        Assert.True(actual[1].SequenceNumber > actual[0].SequenceNumber);
        Assert.True(actual[1].EnqueuedTime >= actual[0].EnqueuedTime);
        Assert.NotEqual(actual[0].OffsetString, actual[1].OffsetString);
    }

    private static async Task<ContextProbeObservation> RunContextProbeAsync(string purpose, int messageCount)
    {
        const string eventHubName = "config-eh";
        var state = new ContextProbeState(NewId.NextGuid(), messageCount);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create(purpose);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<ContextProbeConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("context");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConcurrentDeliveryLimit = 1;
                            endpoint.ConfigureConsumer<ContextProbeConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName).WaitAsync(fixture.OperationTimeout, cancellationToken);
            for (int index = 0; index < messageCount; index++)
            {
                await producer.ProduceAsync<IContextProbeMessage>(
                        new ContextProbeMessage(state.RunId, index),
                        Pipe.Execute<SendContext>(context =>
                        {
                            context.SetPartitionKey(state.PartitionKey);
                            context.Headers.Set("run-id", state.RunId.ToString("N"));
                        }),
                        cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
            }

            await state.Completed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            return new ContextProbeObservation(
                state.RunId,
                state.PartitionKey,
                state.Snapshots.ToArray());
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface IRawInteropMessage
    {
        Guid RunId { get; }
        string Text { get; }
    }

    private sealed record RawInteropMessage(Guid RunId, string Text) : IRawInteropMessage;
    private sealed record RawBusPing(Guid RunId);

    private sealed class RawInteropState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource<ConsumeContext<IRawInteropMessage>> Received { get; } =
            NewSignal<ConsumeContext<IRawInteropMessage>>();
        public TaskCompletionSource<ConsumeContext<RawBusPing>> Ping { get; } =
            NewSignal<ConsumeContext<RawBusPing>>();
    }

    private sealed class RawInteropConsumer(RawInteropState state) : IConsumer<IRawInteropMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<IRawInteropMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return;
            state.Received.TrySetResult(context);
            await context.Advanced().PublishAsync(new RawBusPing(context.Message.RunId), context.CancellationToken);
        }
    }

    private sealed class RawBusPingConsumer(RawInteropState state) : IConsumer<RawBusPing>
    {
        public Task ConsumeAsync(ConsumeContext<RawBusPing> context)
        {
            if (context.Message.RunId == state.RunId)
                state.Ping.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    public interface IDefaultSerializerMessage
    {
        Guid RunId { get; }
        string Text { get; }
    }

    private sealed record DefaultSerializerMessage(Guid RunId, string Text) : IDefaultSerializerMessage;

    private sealed class DefaultSerializerState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource<IDefaultSerializerMessage> Received { get; } = NewSignal<IDefaultSerializerMessage>();
    }

    private sealed class DefaultSerializerConsumer(DefaultSerializerState state) : IConsumer<IDefaultSerializerMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IDefaultSerializerMessage> context)
        {
            if (context.Message.RunId == state.RunId)
                state.Received.TrySetResult(context.Message);
            return Task.CompletedTask;
        }
    }

    public interface IContextProbeMessage
    {
        Guid RunId { get; }
        int Index { get; }
    }

    private sealed record ContextProbeMessage(Guid RunId, int Index) : IContextProbeMessage;
    private sealed record ContextSnapshot(
        int Index,
        bool PayloadPresent,
        string PartitionId,
        string OffsetString,
        long SequenceNumber,
        string? PartitionKey,
        DateTimeOffset EnqueuedTime,
        IReadOnlyDictionary<string, object> SystemProperties,
        IReadOnlyDictionary<string, object?> Properties);

    private sealed record ContextProbeObservation(
        Guid RunId,
        string PartitionKey,
        ContextSnapshot[] Snapshots)
    {
        public bool PayloadPresent => Snapshots.Length > 0 && Snapshots.All(snapshot => snapshot.PayloadPresent);
        public string? PartitionId => Snapshots.FirstOrDefault()?.PartitionId;
        public string OffsetString => Snapshots.FirstOrDefault()?.OffsetString ?? string.Empty;
    }

    private sealed class ContextProbeState(Guid runId, int expected)
    {
        public Guid RunId { get; } = runId;
        public string PartitionKey { get; } = $"context-{runId:N}";
        public ConcurrentQueue<ContextSnapshot> Snapshots { get; } = [];
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Expected { get; } = expected;
    }

    private sealed class ContextProbeConsumer(ContextProbeState state) : IConsumer<IContextProbeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IContextProbeMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return Task.CompletedTask;

            bool found = context.TryGetPayload<EventHubConsumeContext>(out EventHubConsumeContext? payload);
            Assert.True(found);
            Assert.NotNull(payload);
            state.Snapshots.Enqueue(new ContextSnapshot(
                context.Message.Index,
                found,
                payload.PartitionId,
                payload.OffsetString,
                payload.SequenceNumber,
                payload.PartitionKey,
                payload.EnqueuedTime,
                payload.SystemProperties,
                payload.Properties.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal)));
            if (state.Snapshots.Count == state.Expected)
                state.Completed.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
