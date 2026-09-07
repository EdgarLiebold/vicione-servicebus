using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests.EventHubIntegration;

public sealed class EventHubEndpointAndBusBoundaryTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0145", "event-produced-before-dynamic-endpoint-connect-is-delivered-with-complete-envelope")]
    public async Task DynamicEndpoint_ReceivesTheExactEventProducedBeforeItWasConnectedAsync()
    {
        const string eventHubName = "envelope-eh";
        var state = new DynamicEndpointState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("dynamic-connect");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<DynamicEndpointConsumer>();
                    rider.UsingEventHub((_, eventHubs) => fixture.Configure(eventHubs));
                });
            })
            .BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IHostReceiveEndpointHandle? connected = null;
        bool started = false;
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        Guid initiatorId = NewId.NextGuid();
        Guid conversationId = NewId.NextGuid();

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.ProduceAsync<IDynamicEndpointMessage>(
                    new DynamicEndpointMessage(state.Marker, "before-connect"),
                    Pipe.Execute<SendContext>(context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                        context.InitiatorId = initiatorId;
                        context.ConversationId = conversationId;
                        context.Headers.Set("Dynamic", new DynamicHeader("alpha", 17));
                    }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            IEventHubRider rider = provider.GetRequiredService<IEventHubRider>();
            connected = rider.ConnectEventHubEndpoint(
                eventHubName,
                EventHubLocalFixture.ConsumerGroup,
                (context, endpoint) =>
                {
                    endpoint.ContainerName = fixture.ContainerName("dynamic");
                    endpoint.CheckpointMessageCount = 1;
                    endpoint.ConfigureConsumer<DynamicEndpointConsumer>(context);
                });
            await connected.Ready.WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<IDynamicEndpointMessage> actual = await state.Received.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(state.Marker, actual.Message.Marker);
            Assert.Equal("before-connect", actual.Message.Text);
            Assert.Equal(messageId, actual.MessageId);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.Equal(initiatorId, actual.InitiatorId);
            Assert.Equal(conversationId, actual.ConversationId);
            Assert.Equal(new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/{eventHubName}"), actual.DestinationAddress);
            IDynamicHeader header = Assert.IsAssignableFrom<IDynamicHeader>(actual.Headers.Get<IDynamicHeader>("Dynamic"));
            Assert.Equal(("alpha", 17), (header.Name, header.Code));
        }
        finally
        {
            if (connected is not null)
                await connected.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0150", "two-bound-riders-route-first-consume-through-second-provider-without-cross-talk")]
    public async Task TwoBoundRiders_ConsumeOnTheFirstAndProduceThroughTheSecondWithoutCrossTalkAsync()
    {
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("multibus");
        var state = new MultiBusState(NewId.NextGuid());
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(state);
        services
            .AddViciOneServiceBus<IFirstEventHubBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<FirstBusConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint("multibus-eh1", EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("first");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<FirstBusConsumer>(context);
                        });
                    });
                });
            })
            .AddViciOneServiceBus<ISecondEventHubBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<SecondBusConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ReceiveEndpoint("multibus-eh2", EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("second");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<SecondBusConsumer>(context);
                        });
                    });
                });
            });
        await using ServiceProvider provider = services.BuildServiceProvider(true);
        IHostedService[] hostedServices = provider.GetServices<IHostedService>().ToArray();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        int started = 0;

        try
        {
            foreach (IHostedService hostedService in hostedServices)
            {
                await hostedService.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
                started++;
            }

            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducerProvider firstProvider = scope.ServiceProvider
                .GetRequiredService<Bind<IFirstEventHubBus, IEventHubProducerProvider>>().Value;
            IEventHubProducer firstProducer = await firstProvider.GetProducerAsync("multibus-eh1", cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await firstProducer.ProduceAsync(new FirstBusMessage(state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            FirstBusObservation first = await state.First.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            SecondBusObservation second = await state.Second.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(state.RunId, first.RunId);
            Assert.Equal(new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/multibus-eh1"), first.DestinationAddress);
            Assert.Equal(state.RunId, second.RunId);
            Assert.Equal(first.MessageId, second.CausedByMessageId);
            Assert.Equal(new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/multibus-eh2"), second.DestinationAddress);
        }
        finally
        {
            for (int index = started - 1; index >= 0; index--)
                await hostedServices[index].StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0151", "eventhub-inbox-outbox-carries-identifiers-through-bus-default-serializer-once")]
    public async Task RiderInboxOutbox_PublishesOnceThroughTheBusSerializerWithIncomingIdentifiersAsync()
    {
        const string eventHubName = "raw-eh";
        var state = new OutboxState(NewId.NextGuid());
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("outbox");
        var services = new ServiceCollection();
        services.AddSingleton(state);
        services.AddInMemoryInboxOutbox();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.AddConsumer<OutboxBusConsumer>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
            configuration.AddRider(rider =>
            {
                rider.AddConsumer<OutboxRiderConsumer>();
                rider.UsingEventHub((context, eventHubs) =>
                {
                    fixture.Configure(eventHubs);
                    eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                    {
                        endpoint.ContainerName = fixture.ContainerName("outbox");
                        endpoint.CheckpointMessageCount = 1;
                        endpoint.UseInMemoryInboxOutbox(context);
                        endpoint.ConfigureConsumer<OutboxRiderConsumer>(context);
                    });
                });
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(true);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            IEventHubProducer producer = await scope.ServiceProvider.GetRequiredService<IEventHubProducerProvider>()
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            await producer.ProduceAsync<IOutboxRiderMessage>(
                    new OutboxRiderMessage(state.RunId),
                    Pipe.Execute<SendContext>(context =>
                    {
                        context.MessageId = messageId;
                        context.CorrelationId = correlationId;
                    }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<OutboxBusMessage> actual = await state.Delivered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;

            Assert.Equal(state.RunId, actual.Message.RunId);
            Assert.Equal(messageId, actual.Message.OriginalMessageId);
            Assert.Equal(correlationId, actual.Message.OriginalCorrelationId);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, actual.Advanced().ReceiveContext.ContentType);
            Assert.Single(state.Deliveries);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0152", "send-filter-sees-typed-and-untyped-eventhub-payloads-and-continues-delivery")]
    public async Task SendFilter_SeesBothEventHubPayloadShapesAndContinuesTheProviderPipelineAsync()
    {
        const string eventHubName = "config-eh";
        var state = new FilterState(NewId.NextGuid());
        var filter = new EventHubSendFilter(state);
        await using EventHubLocalFixture fixture = EventHubLocalFixture.Create("send-filter");
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(state)
            .AddViciOneServiceBus(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.UsingInMemory();
                configuration.AddRider(rider =>
                {
                    rider.AddConsumer<FilterMessageConsumer>();
                    rider.UsingEventHub((context, eventHubs) =>
                    {
                        fixture.Configure(eventHubs);
                        eventHubs.ConfigureSend(send => send.UseFilter(filter));
                        eventHubs.ReceiveEndpoint(eventHubName, EventHubLocalFixture.ConsumerGroup, endpoint =>
                        {
                            endpoint.ContainerName = fixture.ContainerName("filter");
                            endpoint.CheckpointMessageCount = 1;
                            endpoint.ConfigureConsumer<FilterMessageConsumer>(context);
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
                .GetProducerAsync(eventHubName, cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await producer.ProduceAsync<IFilterMessage>(new FilterMessage(state.RunId), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            FilterObservation filterObservation = await state.Filtered.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            Assert.Equal(state.RunId, await state.Delivered.Task.WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.True(filterObservation.UntypedPayload);
            Assert.True(filterObservation.TypedPayload);
            Assert.Equal(new Uri($"loopback://localhost/{EventHubEndpointAddress.PathPrefix}/{eventHubName}"), filterObservation.DestinationAddress);
            Assert.True(filterObservation.NextCompleted);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public interface IDynamicEndpointMessage
    {
        Guid Marker { get; }
        string Text { get; }
    }

    public interface IDynamicHeader
    {
        string Name { get; }
        int Code { get; }
    }

    private sealed record DynamicEndpointMessage(Guid Marker, string Text) : IDynamicEndpointMessage;
    private sealed record DynamicHeader(string Name, int Code) : IDynamicHeader;

    private sealed class DynamicEndpointState(Guid marker)
    {
        public Guid Marker { get; } = marker;
        public TaskCompletionSource<ConsumeContext<IDynamicEndpointMessage>> Received { get; } =
            NewSignal<ConsumeContext<IDynamicEndpointMessage>>();
    }

    private sealed class DynamicEndpointConsumer(DynamicEndpointState state) : IConsumer<IDynamicEndpointMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IDynamicEndpointMessage> context)
        {
            if (context.Message.Marker == state.Marker)
                state.Received.TrySetResult(context);
            return Task.CompletedTask;
        }
    }

    public interface IFirstEventHubBus : IBus;
    public interface ISecondEventHubBus : IBus;

    private sealed record FirstBusMessage(Guid RunId);
    private sealed record SecondBusMessage(Guid RunId, Guid? CausedByMessageId);
    private sealed record FirstBusObservation(Guid RunId, Guid? MessageId, Uri? DestinationAddress);
    private sealed record SecondBusObservation(Guid RunId, Guid? CausedByMessageId, Uri? DestinationAddress);

    private sealed class MultiBusState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource<FirstBusObservation> First { get; } = NewSignal<FirstBusObservation>();
        public TaskCompletionSource<SecondBusObservation> Second { get; } = NewSignal<SecondBusObservation>();
    }

    private sealed class FirstBusConsumer(
        MultiBusState state,
        Bind<ISecondEventHubBus, IEventHubProducerProvider> secondProvider) : IConsumer<FirstBusMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<FirstBusMessage> context)
        {
            if (context.Message.RunId != state.RunId)
                return;

            state.First.TrySetResult(new FirstBusObservation(context.Message.RunId, context.MessageId, context.DestinationAddress));
            IEventHubProducer producer = await secondProvider.Value.GetProducerAsync("multibus-eh2");
            await producer.ProduceAsync(new SecondBusMessage(context.Message.RunId, context.MessageId), context.CancellationToken);
        }
    }

    private sealed class SecondBusConsumer(MultiBusState state) : IConsumer<SecondBusMessage>
    {
        public Task ConsumeAsync(ConsumeContext<SecondBusMessage> context)
        {
            if (context.Message.RunId == state.RunId)
                state.Second.TrySetResult(new SecondBusObservation(
                    context.Message.RunId,
                    context.Message.CausedByMessageId,
                    context.DestinationAddress));
            return Task.CompletedTask;
        }
    }

    public interface IOutboxRiderMessage
    {
        Guid RunId { get; }
    }

    private sealed record OutboxRiderMessage(Guid RunId) : IOutboxRiderMessage;
    private sealed record OutboxBusMessage(Guid RunId, Guid? OriginalMessageId, Guid? OriginalCorrelationId);

    private sealed class OutboxState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public ConcurrentQueue<OutboxBusMessage> Deliveries { get; } = [];
        public TaskCompletionSource<ConsumeContext<OutboxBusMessage>> Delivered { get; } =
            NewSignal<ConsumeContext<OutboxBusMessage>>();
    }

    private sealed class OutboxRiderConsumer : IConsumer<IOutboxRiderMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IOutboxRiderMessage> context) => context.Advanced().PublishAsync(
            new OutboxBusMessage(context.Message.RunId, context.MessageId, context.CorrelationId),
            context.CancellationToken);
    }

    private sealed class OutboxBusConsumer(OutboxState state) : IConsumer<OutboxBusMessage>
    {
        public Task ConsumeAsync(ConsumeContext<OutboxBusMessage> context)
        {
            if (context.Message.RunId == state.RunId)
            {
                state.Deliveries.Enqueue(context.Message);
                state.Delivered.TrySetResult(context);
            }
            return Task.CompletedTask;
        }
    }

    public interface IFilterMessage
    {
        Guid RunId { get; }
    }

    private sealed record FilterMessage(Guid RunId) : IFilterMessage;
    private sealed record FilterObservation(bool UntypedPayload, bool TypedPayload, Uri? DestinationAddress, bool NextCompleted);

    private sealed class FilterState(Guid runId)
    {
        public Guid RunId { get; } = runId;
        public TaskCompletionSource<FilterObservation> Filtered { get; } = NewSignal<FilterObservation>();
        public TaskCompletionSource<Guid> Delivered { get; } = NewSignal<Guid>();
    }

    private sealed class EventHubSendFilter(FilterState state) : IFilter<SendContext>
    {
        public async Task SendAsync(SendContext context, IPipe<SendContext> next)
        {
            bool untyped = context.TryGetPayload<EventHubSendContext>(out _);
            bool typed = context.TryGetPayload<EventHubSendContext<IFilterMessage>>(out _);
            Uri? destinationAddress = context.DestinationAddress;
            await next.SendAsync(context);
            state.Filtered.TrySetResult(new FilterObservation(untyped, typed, destinationAddress, NextCompleted: true));
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("eventHub-test-observer");
    }

    private sealed class FilterMessageConsumer(FilterState state) : IConsumer<IFilterMessage>
    {
        public Task ConsumeAsync(ConsumeContext<IFilterMessage> context)
        {
            if (context.Message.RunId == state.RunId)
                state.Delivered.TrySetResult(context.Message.RunId);
            return Task.CompletedTask;
        }
    }
}
