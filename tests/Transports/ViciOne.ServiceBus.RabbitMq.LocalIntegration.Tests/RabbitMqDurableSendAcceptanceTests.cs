using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

#nullable enable

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqDurableSendAcceptanceTests
{
    private static readonly MessageContractIdentity ContractIdentity = new("vicione.tests.rabbitmq-durable", 1);

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "confirm-persistent-mandatory-and-restart-retention")]
    public async Task TransportAcceptance_IsPublisherConfirmedPersistentAndRetainedAfterSenderStopAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durableaccept");
        string queue = fixture.Name("accepted");
        byte[] body = [0, 1, 2, 3, 127, 128, 254, 255];
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        DurableSendId durableSendId = new(NewId.NextGuid());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();

            DurableSendDispatchResult result = await dispatcher.DispatchAsync(
                    CreateContext(durableSendId, new Uri($"queue:{queue}"), body, messageId, correlationId),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(DurableSendCompletionMode.TransportAcceptance, result.CompletionMode);
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;

            IReadOnlyList<RabbitMqBroker.RawMessage> messages = await fixture.GetRawAsync(queue, 2, cancellationToken);
            RabbitMqBroker.RawMessage message = Assert.Single(messages);
            Assert.Equal(body, message.Body);
            Assert.True(message.Properties.Persistent);
            Assert.Equal(MediaTypeNames.Application.Json, message.Properties.ContentType);
            Assert.Equal(messageId.ToString("D"), message.Properties.MessageId);
            Assert.Equal(correlationId.ToString("D"), message.Properties.CorrelationId);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "mandatory-unroutable-publish-is-not-accepted")]
    public async Task UnroutablePublish_DoesNotReportTransportAcceptanceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durablereject");
        string exchange = fixture.Name("unroutable");
        DurableSendId durableSendId = new(NewId.NextGuid());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();

            await Assert.ThrowsAsync<MessageReturnedException>(() => dispatcher.DispatchAsync(
                    CreateContext(durableSendId, new Uri($"exchange:{exchange}"), new byte[] { 42 }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "caller-cancellation-prevents-provider-acceptance")]
    public async Task CanceledAttempt_DoesNotPublishOrReportAcceptanceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durablecancel");
        string queue = fixture.Name("canceled");
        DurableSendId durableSendId = new(NewId.NextGuid());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await fixture.DeclareEndpointTopologyAsync(queue, cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchAsync(
                CreateContext(durableSendId, new Uri($"queue:{queue}"), new byte[] { 99 }),
                canceled.Token));
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static ServiceProvider CreateProvider(RabbitMqBroker fixture)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddViciOneMessageContracts(catalog =>
            catalog.Register<DurableAcceptanceMessage>(ContractIdentity.Name, ContractIdentity.MajorVersion));
        services.AddViciOneServiceBus(configurator =>
        {
            configurator.Limits(MessageLimits.Conservative);
            configurator.UseReliableMessaging(reliable =>
            {
                reliable.UseInMemoryStore();
                reliable.Store(new ReliableStoreLimits
                {
                    MaximumStoredCount = 100,
                    MaximumStoredBytes = 1024 * 1024,
                });
                reliable.Delivery(_ => { });
                reliable.Retention(TimeSpan.FromDays(1));
            });
            configurator.UsingRabbitMq((_, rabbit) => fixture.ConfigureHost(rabbit));
        });
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }

    private static DurableSendDispatchContext CreateContext(
        DurableSendId id,
        Uri destination,
        ReadOnlyMemory<byte> body,
        Guid? messageId = null,
        Guid? correlationId = null) =>
        new(
            new SerializedDurableSend
            {
                Id = id,
                ContractIdentity = ContractIdentity,
                DestinationAddress = destination,
                ContentType = MediaTypeNames.Application.Json,
                Body = body,
                MessageId = messageId,
                CorrelationId = correlationId,
            },
            id,
            Attempt: 1,
            new UnusedConsumerCompletion(id));

    private sealed record DurableAcceptanceMessage(Guid Identity);

    private sealed class UnusedConsumerCompletion(DurableSendId durableSendId) : IDurableSendConsumerCompletion
    {
        public DurableSendId DurableSendId { get; } = durableSendId;

        public ValueTask<bool> CompleteAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return global::System.Threading.Tasks.ValueTask.FromCanceled<bool>(cancellationToken);

            return ValueTask.FromException<bool>(new InvalidOperationException(
                "RabbitMQ transport acceptance must never invoke the in-process consumer-completion capability."));
        }
    }
}
