using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Diagnostics;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.MessagePack.Tests.Configuration;

public sealed class MessagePackTransportIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-CONFIGURATION", "factory-shares-serializer")]
    public void Factory_UsesOneSerializerForBothDirections()
    {
        var factory = new MessagePackSerializerFactory();

        var serializer = factory.CreateSerializer();
        var deserializer = factory.CreateDeserializer();

        Assert.Same(serializer, deserializer);
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, factory.ContentType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-DURABLE-SEND", "typed-facade-canonical-envelope-and-idempotent-retry")]
    public async Task TypedDurableSender_UsesTheConfiguredMessagePackEnvelopeWithoutReserializationAsync()
    {
        var destination = new Uri("loopback://messagepack-durable/input");
        var durableId = new DurableSendId(Guid.Parse("9358cc89-9ff0-4ef4-8202-835f98ef5e09"));
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.UsingInMemory((_, transport) =>
            {
                transport.Host(new Uri("loopback://messagepack-durable/"));
                transport.ClearSerialization();
                transport.UseMessagePackSerializer();
                transport.Route<DurableMessagePackMessage>(destination);
            });
            configuration.UseDurableSender(durable =>
            {
                durable.UseInMemoryStore();
                durable.AddMessageContract<DurableMessagePackMessage>("vicione.tests.messagepack-durable");
            });
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IDurableSender<IBus> sender = scope.ServiceProvider.GetRequiredService<IDurableSender<IBus>>();
        var message = new DurableMessagePackMessage { Value = "native-messagepack" };
        var options = new DurableSendOptions { IdempotencyKey = durableId };

        DurableSendReceipt first = await sender.SendAsync(message, options, TestContext.Current.CancellationToken);
        DurableSendReceipt duplicate = await sender.SendAsync(destination, message, options, TestContext.Current.CancellationToken);

        Assert.True(first.IsNew);
        Assert.Equal(DurableSendAdmissionDisposition.AlreadyAccepted, duplicate.Disposition);
        IDurableSendStore<IBus> store = scope.ServiceProvider.GetRequiredService<IDurableSendStore<IBus>>();
        DurableSendDelivery retained = Assert.Single(await store.ClaimDueAsync(
            DateTimeOffset.UtcNow.AddDays(1),
            1,
            TimeSpan.FromMinutes(1),
            TestContext.Current.CancellationToken));
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType.ToString(), retained.Message.ContentType);
        Assert.Equal(durableId.Value, retained.Message.MessageId);
        Assert.False(retained.Message.Body.IsEmpty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "in-memory-pipeline-dispatch")]
    public async Task InterfaceMessage_DispatchesThroughTheConfiguredInMemoryPipelineAsync()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
                configuration.UsingInMemory((context, transport) =>
                {
                    transport.ClearSerialization();
                    transport.UseMessagePackSerializer();
                    transport.ConfigureEndpoints(context);
                }))
            .BuildServiceProvider(validateScopes: true);
        var harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken);

        try
        {
            Task<ConsumeContext<InterfaceDispatchMessage>> received =
                await harness.ConnectPublishHandlerAsync<InterfaceDispatchMessage>(_ => true, cancellationToken: TestContext.Current.CancellationToken);
            await harness.Bus.PublishAsync<InterfaceDispatchMessage>(
                new { Value = "preserved" },
                TestContext.Current.CancellationToken);

            var context = await received.WaitAsync(
                harness.TestTimeout,
                TestContext.Current.CancellationToken);

            Assert.Equal("preserved", context.Message.Value);
            Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, context.Advanced().ReceiveContext.ContentType);
            Assert.True(await harness.Consumed.AnyAsync<InterfaceDispatchMessage>(
                TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "expired-discarded-before-serialization")]
    public async Task ExpiredForwardedMessage_IsDiscardedBeforeMessagePackSerializationAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"messagepack-forwarding-expiration-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        var sourceCompleted = new TaskCompletionSource<ConsumeContext<ForwardExpirationMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var projection = new TaskCompletionSource<ForwardExpirationProjection>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var destinationDeliveryCount = 0;
        Uri forwardAddress = new(harness.BaseAddress, "messagepack-expiration-forward");
        var observer = new DestinationSendObserver(forwardAddress);

        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
            configurator.Handler<ForwardExpirationMessage>(async context =>
            {
                DateTimeOffset expiration = Assert.IsType<DateTimeOffset>(context.ExpirationTime).ToUniversalTime();
                var timeProvider = new FakeTimeProvider(expiration.AddMinutes(1));
                context.SetTimeProvider(timeProvider);
                await context.ForwardAsync(
                        forwardAddress,
                        Pipe.Execute<SendContext<ForwardExpirationMessage>>(sendContext =>
                            projection.TrySetResult(new ForwardExpirationProjection(
                                sendContext.TimeToLive,
                                timeProvider.GetUtcNow().UtcDateTime))))
                    .ConfigureAwait(false);
                sourceCompleted.TrySetResult(context);
            });
        harness.OnConfigureInMemoryBus += configurator =>
        {
            configurator.ClearSerialization();
            configurator.UseMessagePackSerializer();
            configurator.ReceiveEndpoint("messagepack-expiration-forward", endpoint =>
                endpoint.Handler<ForwardExpirationMessage>(_ =>
                {
                    Interlocked.Increment(ref destinationDeliveryCount);
                    return Task.CompletedTask;
                }));
        };

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            await harness.InputQueueSendEndpoint.SendAsync(
                    new ForwardExpirationMessage { Value = "expired" },
                    context => context.TimeToLive = TimeSpan.FromMinutes(5),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ForwardExpirationMessage> source = await sourceCompleted.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ForwardExpirationProjection projected = await projection.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.True(Assert.IsType<DateTimeOffset>(source.ExpirationTime) < projected.CapturedAtUtc);
            Assert.True(Assert.IsType<TimeSpan>(projected.TimeToLive) < TimeSpan.Zero);
            Assert.Equal(0, Volatile.Read(ref destinationDeliveryCount));
            Assert.Equal(0, observer.PreSendCount);
            Assert.Equal(0, observer.PostSendCount);
            Assert.Equal(0, observer.SendFaultCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-REDELIVERY", "messagepack-envelope-remains-consumable")]
    public async Task DelayedRedelivery_PreservesMessageTypeAndReachesTheSecondDeliveryAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan interval = TimeSpan.FromHours(1);
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<FaultOnceConsumer>();
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.Intervals(interval);
                        redelivery.ReplaceMessageId = true;
                    }));
                configuration.UsingInMemory((context, transport) =>
                {
                    transport.ClearSerialization();
                    transport.UseMessagePackSerializer();
                    transport.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        var harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        var scheduled = new MessagePackScheduledObserver();
        using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(scheduled);
        Guid originalMessageId = Guid.Parse("9d004f10-c5a8-42f7-bfd0-bf5df26fab78");
        IList<IReceivedMessage<RetryMessage>> deliveries;

        try
        {
            await harness.Bus.PublishAsync(
                new RetryMessage { Value = "preserved" },
                context => context.MessageId = originalMessageId,
                cancellationToken);

            SendContext scheduledContext = await scheduled.Scheduled.WaitAsync(timeout, cancellationToken);
            Assert.Equal(interval, scheduledContext.Delay);
            provider.GetRequiredService<IInMemoryDelayProvider>().Advance(interval);

            Assert.True(await harness.Published.AnyAsync<CompletedMessage>(cancellationToken));
            deliveries = await harness.Consumed
                .SelectAsync<RetryMessage>(cancellationToken)
                .Take(2)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, deliveries.Count);
        Assert.All(deliveries, delivery =>
        {
            Assert.Equal("preserved", delivery.Context.Message.Value);
            Assert.Equal(
                MessagePackMessageSerializer.MessagePackContentType,
                delivery.Context.Advanced().ReceiveContext.ContentType);
            Assert.Contains(
                MessageUrn.ForTypeString<RetryMessage>(),
                delivery.Context.Advanced().SupportedMessageTypes);
        });
        Assert.Equal(originalMessageId, deliveries[0].Context.MessageId);
        Assert.NotNull(deliveries[1].Context.MessageId);
        Assert.NotEqual(deliveries[0].Context.MessageId, deliveries[1].Context.MessageId);
        Assert.Equal(0, deliveries[0].Context.Advanced().GetRedeliveryCount());
        Assert.Equal(1, deliveries[1].Context.Advanced().GetRedeliveryCount());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-MIXED-SERIALIZERS", "json-request-messagepack-response")]
    public async Task MixedSerializers_PreserveEachDirectionAndExactContentTypeAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var pingReceived = new TaskCompletionSource<ConsumeContext<MixedPing>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(operationTimeout, operationTimeout);
                configuration.AddHandler<MixedPing>(async context =>
                {
                    pingReceived.TrySetResult(context);
                    await context.RespondAsync(
                        new MixedPong(context.Message.CorrelationId, "messagepack"));
                });
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseMessagePackSerializer());
                configuration.UsingInMemory((context, transport) =>
                {
                    transport.UseMessagePackDeserializer();
                    transport.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(operationTimeout, cancellationToken);
        Guid correlationId = Guid.Parse("a22b393a-5a4b-447b-bdc6-52089d5736a3");
        ConsumeContext<MixedPing> ping;
        ConsumeContext<MixedPong> pong;

        try
        {
            Task<ConsumeContext<MixedPong>> pongReceived =
                await harness.ConnectPublishHandlerAsync<MixedPong>(_ => true, cancellationToken: TestContext.Current.CancellationToken);
            await harness.Bus.PublishAsync(
                    new MixedPing(correlationId, "json"),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
            ping = await pingReceived.Task.WaitAsync(operationTimeout, cancellationToken);
            pong = await pongReceived.WaitAsync(operationTimeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None)
                .WaitAsync(operationTimeout, CancellationToken.None);
        }

        Assert.Equal(correlationId, ping.Message.CorrelationId);
        Assert.Equal("json", ping.Message.Value);
        Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, ping.Advanced().ReceiveContext.ContentType);
        Assert.Equal(correlationId, pong.Message.CorrelationId);
        Assert.Equal("messagepack", pong.Message.Value);
        Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, pong.Advanced().ReceiveContext.ContentType);
    }

    private sealed class FaultOnceConsumer : IConsumer<RetryMessage>
    {
        public async Task ConsumeAsync(ConsumeContext<RetryMessage> context)
        {
            if (context.Advanced().GetRedeliveryCount() == 0)
            {
                throw new ExpectedRedeliveryException();
            }

            await context.Advanced().PublishAsync(
                new CompletedMessage { Value = context.Message.Value },
                context.CancellationToken);
        }
    }

    private sealed class RetryMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class CompletedMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed record MixedPing(Guid CorrelationId, string Value);
    public sealed record MixedPong(Guid CorrelationId, string Value);

    public sealed class DurableMessagePackMessage
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed class ForwardExpirationMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ForwardExpirationProjection(
        TimeSpan? TimeToLive,
        DateTime CapturedAtUtc);

    private sealed class ExpectedRedeliveryException : Exception;

    private sealed class MessagePackScheduledObserver : ISendObserver
    {
        private readonly TaskCompletionSource<SendContext> _scheduled = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<SendContext> Scheduled => _scheduled.Task;

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (typeof(T) == typeof(RetryMessage) && context.Delay.HasValue)
                _scheduled.TrySetResult(context);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (typeof(T) == typeof(RetryMessage) && context.Delay.HasValue)
                _scheduled.TrySetException(exception);

            return Task.CompletedTask;
        }
    }

    private sealed class DestinationSendObserver(Uri destinationAddress) : ISendObserver
    {
        private int _postSendCount;
        private int _preSendCount;
        private int _sendFaultCount;

        public int PostSendCount => Volatile.Read(ref _postSendCount);
        public int PreSendCount => Volatile.Read(ref _preSendCount);
        public int SendFaultCount => Volatile.Read(ref _sendFaultCount);

        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.DestinationAddress == destinationAddress)
                Interlocked.Increment(ref _preSendCount);

            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.DestinationAddress == destinationAddress)
                Interlocked.Increment(ref _postSendCount);

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            if (context.DestinationAddress == destinationAddress)
                Interlocked.Increment(ref _sendFaultCount);

            return Task.CompletedTask;
        }
    }

    public interface InterfaceDispatchMessage
    {
        string Value { get; }
    }
}
