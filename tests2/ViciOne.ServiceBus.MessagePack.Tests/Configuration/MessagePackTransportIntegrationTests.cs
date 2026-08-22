using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
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
            Assert.True(await harness.Consumed.Any<InterfaceDispatchMessage>(
                TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.Stop(TestContext.Current.CancellationToken);
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

    private sealed class ExpectedRedeliveryException : Exception;

    public interface InterfaceDispatchMessage
    {
        string Value { get; }
    }
}
