using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubProducerDeliveryTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0143", "ten-item-produce-preserves-shared-envelope-and-exact-cardinality")]
    public async Task BatchProduce_DeliversEveryItemOnceWithTheSharedEnvelopeAsync()
    {
        const string eventHubName = "batch-eh";
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("batch-produce");
        string containerName = fixture.ContainerName("checkpoint");
        var observations = new ConcurrentQueue<EnvelopeObservation>();
        var allReceived = NewObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observations)
            .AddSingleton(allReceived)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<BatchEnvelopeConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = containerName;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<BatchEnvelopeConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        Guid initiatorId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider
                .GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            BatchEnvelopeMessage[] messages = Enumerable.Range(0, 10)
                .Select(index => new BatchEnvelopeMessage(index, $"item-{index}"))
                .ToArray();

            await producer.ProduceAsync<IEnvelopeMessage>(
                    messages,
                    Pipe.Execute<SendContext>(context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.InitiatorId = initiatorId;
                        context.ConversationId = conversationId;
                    }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await allReceived.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            EnvelopeObservation[] actual = observations.OrderBy(item => item.Index).ToArray();
            Assert.Equal(10, actual.Length);
            Assert.Equal(Enumerable.Range(0, 10), actual.Select(item => item.Index));
            Assert.Equal(Enumerable.Range(0, 10).Select(index => $"item-{index}"), actual.Select(item => item.Text));
            Assert.All(actual, item =>
            {
                Assert.Equal(messageId, item.MessageId);
                Assert.Equal(correlationId, item.CorrelationId);
                Assert.Equal(initiatorId, item.InitiatorId);
                Assert.Equal(conversationId, item.ConversationId);
            });
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0154", "single-produce-round-trips-full-envelope-structured-header-and-addresses")]
    public async Task SingleProduce_RoundTripsTheCompleteEnvelopeAndStructuredHeaderAsync()
    {
        const string eventHubName = "envelope-eh";
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("envelope");
        string containerName = fixture.ContainerName("checkpoint");
        Guid marker = NewId.NextGuid();
        var received = NewObservation<ConsumeContext<IEnvelopeMessage>>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(new ExpectedEnvelope(marker, received))
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<SingleEnvelopeConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = containerName;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<SingleEnvelopeConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        Guid initiatorId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider
                .GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.ProduceAsync<IEnvelopeMessage>(
                    new EnvelopeMessage(marker, 41, "complete"),
                    Pipe.Execute<SendContext>(context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.InitiatorId = initiatorId;
                        context.ConversationId = conversationId;
                        context.Headers.Set("Special", new HeaderValue("alpha", "omega"));
                    }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<IEnvelopeMessage> actual = await received.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal((marker, 41, "complete"), (actual.Message.Marker, actual.Message.Index, actual.Message.Text));
            Assert.Equal(messageId, actual.MessageId);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(initiatorId, actual.InitiatorId);
            Assert.Equal(conversationId, actual.ConversationId);
            Assert.Equal(new Uri("loopback://localhost/"), actual.SourceAddress);
            Assert.Equal(
                new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/{eventHubName}"),
                actual.DestinationAddress);
            IHeaderValue header = Assert.IsAssignableFrom<IHeaderValue>(actual.Headers.Get<IHeaderValue>("Special"));
            Assert.Equal(("alpha", "omega"), (header.Key, header.Value));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0155", "bus-send-observer-sees-rider-pre-and-post-around-provider-send")]
    public async Task BusSendObserver_SeesRiderProduceBeforeAndAfterTheProviderSendAsync()
    {
        const string eventHubName = "envelope-eh";
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("observer");
        string containerName = fixture.ContainerName("checkpoint");
        var consumed = NewObservation<Guid>();
        var observer = new RecordingSendObserver();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(consumed)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.UsingInMemory((_, bus) => bus.ConnectSendObserver(observer));
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<ObserverMessageConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = containerName;
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<ObserverMessageConsumer>(context);
                        });
                    });
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid marker = NewId.NextGuid();
        Guid messageId = NewId.NextGuid();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider
                .GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.ProduceAsync<IEnvelopeMessage>(
                    new EnvelopeMessage(marker, 1, "observer"),
                    Pipe.Execute<SendContext>(context => context.MessageId = messageId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(marker, await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            SendObservation[] actual = observer.Snapshot();
            Assert.Equal(["pre", "post"], actual.Select(item => item.Stage));
            Assert.All(actual, item =>
            {
                Assert.Equal(messageId, item.MessageId);
                Assert.Equal(
                    new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/{eventHubName}"),
                    item.DestinationAddress);
            });
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource NewObservation() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface IEnvelopeMessage
    {
        Guid Marker { get; }
        int Index { get; }
        string Text { get; }
    }

    public interface IHeaderValue
    {
        string Key { get; }
        string Value { get; }
    }

    private sealed record EnvelopeMessage(Guid Marker, int Index, string Text) : IEnvelopeMessage;
    private sealed record BatchEnvelopeMessage(int Index, string Text) : IEnvelopeMessage
    {
        public Guid Marker { get; } = Guid.Empty;
    }
    private sealed record HeaderValue(string Key, string Value) : IHeaderValue;
    private sealed record ExpectedEnvelope(Guid Marker, TaskCompletionSource<ConsumeContext<IEnvelopeMessage>> Received);
    private sealed record EnvelopeObservation(
        int Index,
        string Text,
        Guid? MessageId,
        Guid? CorrelationId,
        Guid? InitiatorId,
        Guid? ConversationId);
    private sealed record SendObservation(string Stage, Guid? MessageId, Uri? DestinationAddress);

    private sealed class BatchEnvelopeConsumer(
        ConcurrentQueue<EnvelopeObservation> observations,
        TaskCompletionSource allReceived) : IConsumer<IEnvelopeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IEnvelopeMessage> context)
        {
            observations.Enqueue(new EnvelopeObservation(
                context.Message.Index,
                context.Message.Text,
                context.MessageId,
                context.CorrelationId,
                context.InitiatorId,
                context.ConversationId));
            if (observations.Count == 10)
                allReceived.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class SingleEnvelopeConsumer(ExpectedEnvelope expected) :
        IConsumer<IEnvelopeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IEnvelopeMessage> context)
        {
            if (context.Message.Marker == expected.Marker)
                expected.Received.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    private sealed class ObserverMessageConsumer(TaskCompletionSource<Guid> consumed) : IConsumer<IEnvelopeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IEnvelopeMessage> context)
        {
            consumed.TrySetResult(context.Message.Marker);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSendObserver : ISendObserver
    {
        private readonly object _lock = new();
        private readonly List<SendObservation> _observations = [];

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            Record("pre", context);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class
        {
            Record("post", context);
            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class =>
            Task.FromException(new InvalidOperationException("The successful observer test reached SendFault.", exception));

        public SendObservation[] Snapshot()
        {
            lock (_lock)
                return _observations.ToArray();
        }

        private void Record<T>(string stage, SendContext<T> context) where T : class
        {
            lock (_lock)
                _observations.Add(new SendObservation(stage, context.MessageId, context.DestinationAddress));
        }
    }
}
