using Microsoft.Extensions.DependencyInjection;
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
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-INTERFACES", "in-memory-pipeline-dispatch")]
    public async Task InterfaceMessage_DispatchesThroughTheConfiguredInMemoryPipeline()
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
        var harness = await provider.StartTestHarness();

        try
        {
            Task<ConsumeContext<InterfaceDispatchMessage>> received =
                await harness.ConnectPublishHandler<InterfaceDispatchMessage>(_ => true);
            await harness.Bus.Publish<InterfaceDispatchMessage>(
                new { Value = "preserved" },
                TestContext.Current.CancellationToken);

            var context = await received.WaitAsync(
                harness.TestTimeout,
                TestContext.Current.CancellationToken);

            Assert.Equal("preserved", context.Message.Value);
            Assert.Equal(MessagePackMessageSerializer.MessagePackContentType, context.ReceiveContext.ContentType);
            Assert.True(await harness.Consumed.Any<InterfaceDispatchMessage>(
                TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.Stop(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-FORWARDING", "expired-discarded-before-serialization")]
    public async Task ExpiredForwardedMessage_IsDiscardedBeforeMessagePackSerialization()
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
                await context.Forward(
                        forwardAddress,
                        Pipe.Execute<SendContext<ForwardExpirationMessage>>(sendContext =>
                            projection.TrySetResult(new ForwardExpirationProjection(
                                sendContext.TimeToLive,
                                DateTime.UtcNow))))
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
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            using ConnectHandle observerHandle = harness.Bus.ConnectSendObserver(observer);
            await harness.InputQueueSendEndpoint.Send(
                    new ForwardExpirationMessage { Value = "expired" },
                    context => context.TimeToLive = TimeSpan.FromSeconds(-30),
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            ConsumeContext<ForwardExpirationMessage> source = await sourceCompleted.Task.WaitAsync(
                operationTimeout,
                cancellationToken);
            ForwardExpirationProjection projected = await projection.Task.WaitAsync(
                operationTimeout,
                cancellationToken);

            Assert.True(Assert.IsType<DateTime>(source.ExpirationTime) < projected.CapturedAtUtc);
            Assert.True(Assert.IsType<TimeSpan>(projected.TimeToLive) < TimeSpan.Zero);
            Assert.Equal(0, Volatile.Read(ref destinationDeliveryCount));
            Assert.Equal(0, observer.PreSendCount);
            Assert.Equal(0, observer.PostSendCount);
            Assert.Equal(0, observer.SendFaultCount);
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGEPACK-REDELIVERY", "messagepack-envelope-remains-consumable")]
    public async Task DelayedRedelivery_PreservesMessageTypeAndReachesTheSecondDelivery()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddConsumer<FaultOnceConsumer>();
                configuration.AddConfigureEndpointsCallback((_, _, endpoint) =>
                    endpoint.UseDelayedRedelivery(redelivery =>
                    {
                        redelivery.Intervals(TimeSpan.FromMilliseconds(5));
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
        var harness = await provider.StartTestHarness();

        try
        {
            await harness.Bus.Publish(
                new RetryMessage { Value = "preserved" },
                TestContext.Current.CancellationToken);

            Assert.True(await harness.Published.Any<CompletedMessage>(TestContext.Current.CancellationToken));
            IList<IReceivedMessage<RetryMessage>> deliveries = await harness.Consumed
                .SelectAsync<RetryMessage>(TestContext.Current.CancellationToken)
                .Take(2)
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, deliveries.Count);
            Assert.All(deliveries, delivery =>
            {
                Assert.Equal("preserved", delivery.Context.Message.Value);
                Assert.Equal(
                    MessagePackMessageSerializer.MessagePackContentType,
                    delivery.Context.ReceiveContext.ContentType);
                Assert.Contains(
                    MessageUrn.ForTypeString<RetryMessage>(),
                    delivery.Context.SupportedMessageTypes);
            });
            Assert.Equal(0, deliveries[0].Context.GetRedeliveryCount());
            Assert.Equal(1, deliveries[1].Context.GetRedeliveryCount());
        }
        finally
        {
            await harness.Stop(TestContext.Current.CancellationToken);
        }
    }

    private sealed class FaultOnceConsumer : IConsumer<RetryMessage>
    {
        public async Task Consume(ConsumeContext<RetryMessage> context)
        {
            if (context.GetRedeliveryCount() == 0)
            {
                throw new ExpectedRedeliveryException();
            }

            await context.Publish(
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

    private sealed class ForwardExpirationMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ForwardExpirationProjection(
        TimeSpan? TimeToLive,
        DateTime CapturedAtUtc);

    private sealed class ExpectedRedeliveryException : Exception;

    private sealed class DestinationSendObserver(Uri destinationAddress) : ISendObserver
    {
        private int _postSendCount;
        private int _preSendCount;
        private int _sendFaultCount;

        public int PostSendCount => Volatile.Read(ref _postSendCount);
        public int PreSendCount => Volatile.Read(ref _preSendCount);
        public int SendFaultCount => Volatile.Read(ref _sendFaultCount);

        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            if (context.DestinationAddress == destinationAddress)
                Interlocked.Increment(ref _preSendCount);

            return Task.CompletedTask;
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            if (context.DestinationAddress == destinationAddress)
                Interlocked.Increment(ref _postSendCount);

            return Task.CompletedTask;
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
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
