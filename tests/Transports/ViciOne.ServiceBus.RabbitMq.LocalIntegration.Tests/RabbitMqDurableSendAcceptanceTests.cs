using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.LocalIntegration.Tests;

public sealed class RabbitMqDurableSendAcceptanceTests
{
    private static readonly MessageContractIdentity ContractIdentity = new("vicione.tests.rabbitmq-durable", 1);

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "confirmed-persistent-quorum-queue-and-sender-stop-retention")]
    public async Task TransportAcceptance_IsPublisherConfirmedPersistentAndRetainedAfterSenderStopAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durableaccept");
        string queue = fixture.Name("accepted");
        byte[] body = [0, 1, 2, 3, 127, 128, 254, 255];
        Guid messageId = NewId.NextGuid();
        Guid correlationId = NewId.NextGuid();
        DurableSendId durableSendId = new(NewId.NextGuid());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await DeclareDurableQuorumEndpointAsync(fixture, queue, cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();

            DurableSendDispatchResult result = await dispatcher.DispatchAsync(
                    CreateContext(durableSendId, new Uri(fixture.Address, queue), body, messageId, correlationId),
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
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "missing-quorum-queue-is-not-accepted")]
    public async Task MissingQuorumQueue_DoesNotReportTransportAcceptanceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durablereject");
        string queue = fixture.Name("missing");
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

            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => dispatcher.DispatchAsync(
                    CreateContext(durableSendId, new Uri(fixture.Address, queue), new byte[] { 42 }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
            var brokerReply = Assert.IsType<OperationInterruptedException>(exception.InnerException);
            Assert.NotNull(brokerReply.ShutdownReason);
            Assert.Equal((ushort)404, brokerReply.ShutdownReason.ReplyCode);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "mandatory-unroutable-shared-publish-boundary-rejects-success")]
    public async Task MandatoryUnroutableSharedPublishBoundary_ThrowsReturnedMessageAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("mandatoryreturn");
        string exchange = fixture.Name("unroutable");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint endpoint = await bus.GetSendEndpointAsync(new Uri(fixture.Address, exchange), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            MessageReturnedException exception = await Assert.ThrowsAsync<MessageReturnedException>(() => endpoint.SendAsync(
                    new DurableAcceptanceMessage(NewId.NextGuid()),
                    context =>
                    {
                        RabbitMqSendContext rabbitMqContext = context.GetPayload<RabbitMqSendContext>();
                        rabbitMqContext.Mandatory = true;
                        rabbitMqContext.AwaitAck = true;
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
            Assert.NotNull(exception.InnerException);
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "unbound-classic-queue-is-rejected-without-routing-mutation")]
    public async Task ClassicQueue_DoesNotReportTransportAcceptanceAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durableclassic");
        string queue = fixture.Name("classic");
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await DeclareUnboundClassicEndpointAsync(fixture, queue, cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();

            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => dispatcher.DispatchAsync(
                    CreateContext(new DurableSendId(NewId.NextGuid()), new Uri(fixture.Address, queue), new byte[] { 42 }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));
            var brokerReply = Assert.IsType<OperationInterruptedException>(exception.InnerException);
            Assert.NotNull(brokerReply.ShutdownReason);
            Assert.Equal((ushort)406, brokerReply.ShutdownReason.ReplyCode);
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
            IReadOnlyList<RabbitMqBroker.BindingState> bindings = await fixture.QueueBindingsAsync(queue, cancellationToken);
            Assert.DoesNotContain(bindings, binding =>
                binding.Source == queue
                && binding.Destination == queue
                && binding.DestinationType == "queue");
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
        await DeclareDurableQuorumEndpointAsync(fixture, queue, cancellationToken);
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
                CreateContext(durableSendId, new Uri(fixture.Address, queue), new byte[] { 99 }),
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

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-DURABLE-SEND", "disabled-confirmations-refuse-transport-acceptance")]
    public async Task DisabledPublisherConfirmations_RefuseTransportAcceptanceBeforePublishAsync()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("durablenoconfirm");
        string queue = fixture.Name("not-accepted");
        DurableSendId durableSendId = new(NewId.NextGuid());
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await DeclareDurableQuorumEndpointAsync(fixture, queue, cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture, publisherConfirmation: false);
        IBusControl bus = provider.GetRequiredService<IBusControl>();
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IDurableSendDispatcher<IBus> dispatcher = provider.GetRequiredService<IDurableSendDispatcher<IBus>>();

            ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(() => dispatcher.DispatchAsync(
                    CreateContext(durableSendId, new Uri(fixture.Address, queue), new byte[] { 73 }),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken));

            Assert.Contains("publisher confirmations are disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0U, await fixture.QueueMessageCountAsync(queue, cancellationToken));
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private static async Task DeclareDurableQuorumEndpointAsync(
        RabbitMqBroker fixture,
        string queue,
        CancellationToken cancellationToken)
    {
        ConnectionFactory factory = fixture.CreateConnectionFactory();
        await using IConnection connection = await factory.CreateConnectionAsync(cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await channel.ExchangeDeclareAsync(
                queue, ExchangeType.Fanout, durable: true, autoDelete: false,
                arguments: null, noWait: false, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await channel.QueueDeclareAsync(
                queue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?> { [RabbitMQ.Client.Headers.XQueueType] = "quorum" },
                noWait: false, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await channel.QueueBindAsync(
                queue, queue, routingKey: string.Empty, arguments: null,
                noWait: false, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
    }

    private static async Task DeclareUnboundClassicEndpointAsync(
        RabbitMqBroker fixture,
        string queue,
        CancellationToken cancellationToken)
    {
        ConnectionFactory factory = fixture.CreateConnectionFactory();
        await using IConnection connection = await factory.CreateConnectionAsync(cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await channel.ExchangeDeclareAsync(
                queue, ExchangeType.Fanout, durable: true, autoDelete: false,
                arguments: null, noWait: false, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
        await channel.QueueDeclareAsync(
                queue, durable: true, exclusive: false, autoDelete: false,
                arguments: null, noWait: false, cancellationToken: cancellationToken)
            .WaitAsync(fixture.OperationTimeout, cancellationToken);
    }

    private static ServiceProvider CreateProvider(
        RabbitMqBroker fixture,
        bool publisherConfirmation = true)
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
            configurator.UsingRabbitMq((_, rabbit) => fixture.ConfigureHost(rabbit, publisherConfirmation));
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
            attempt: 1,
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
