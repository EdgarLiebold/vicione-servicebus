using System.Buffers;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DurableSend;

public sealed class InMemoryReliableInboxContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "outgoing-metadata-drift-cannot-be-committed")]
    public async Task OutgoingMetadataDrift_LeavesInboxOutboxEmptyAndRestoresContextAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var runtime = new PayloadAdmissionRuntime<ITestBus>(new PayloadAdmissionEvaluator<ITestBus>(
            new PayloadAdmissionPolicy
            {
                MaximumSerializedBodyBytes = 16384,
                MaximumTransportEnvelopeBytes = 16384,
            }));
        using ServiceProvider services = new ServiceCollection().AddSingleton(runtime).BuildServiceProvider();
        var store = new InMemoryReliableStore<ITestBus>();
        var catalog = new MessageContractCatalogBuilder()
            .Register<OutgoingMessage>("vicione.tests.inbox-outgoing-metadata")
            .Build();
        var key = new ReliableInboxKey(Guid.NewGuid(), Guid.NewGuid());
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        ReliableInboxAcquireResult acquired = await store.AcquireAsync(
            key, now, TimeSpan.FromMinutes(1), token);
        ReliableInboxLease lease = Assert.IsType<ReliableInboxLease>(acquired.Lease);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = key.ConsumerId,
            ConsumerType = nameof(IncomingMessage),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var incoming = InMemoryOutboxTestContextFactory.Create(
            new IncomingMessage(), token, messageId: key.MessageId, serviceProvider: services);
        var inbox = new InMemoryReliableInboxContext<ITestBus, IncomingMessage>(
            incoming, options, services, store, catalog, new DurableSendStoreLimits(10, 65536),
            key, lease, receiveCount: 1, TimeProvider.System);
        var outgoing = new MessageSendContext<OutgoingMessage>(new OutgoingMessage())
        {
            MessageId = Guid.NewGuid(),
            DestinationAddress = new Uri("loopback://inmemory-reliable-inbox/outgoing"),
            Serializer = ServiceBusMetadataJson.MessageSerializer,
        };
        Guid originalCorrelation = Guid.NewGuid();
        int metadataReads = 0;
        outgoing.CorrelationId = originalCorrelation;
        outgoing.Headers.Set("metadata-value", new MetadataMutationHeader(() =>
        {
            if (outgoing.BodyLength.HasValue)
            {
                metadataReads++;
                if (metadataReads == 3)
                    outgoing.CorrelationId = Guid.NewGuid();
            }
        }));

        MessageException failure = await Assert.ThrowsAsync<MessageException>(
            () => inbox.AddSendAsync(outgoing, token));

        Assert.Contains("CorrelationId", failure.Message, StringComparison.Ordinal);
        Assert.True(metadataReads >= 3);
        Assert.Equal(originalCorrelation, outgoing.CorrelationId);
        await inbox.SetConsumedAsync(token);
        Assert.Equal(0, (await store.GetSnapshotAsync(token)).StoredCount);
        Assert.Empty(await store.ClaimDueAsync(now.AddSeconds(1), 10, TimeSpan.FromMinutes(1), token));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RELIABLE-INBOX", "serializer-getter-cannot-change-staged-destination")]
    public async Task SerializerGetterChangingDestination_RejectsBeforeInboxCommitAsync()
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var runtime = new PayloadAdmissionRuntime<ITestBus>(new PayloadAdmissionEvaluator<ITestBus>(
            new PayloadAdmissionPolicy
            {
                MaximumSerializedBodyBytes = 16384,
                MaximumTransportEnvelopeBytes = 16384,
            }));
        using ServiceProvider services = new ServiceCollection().AddSingleton(runtime).BuildServiceProvider();
        var store = new InMemoryReliableStore<ITestBus>();
        var catalog = new MessageContractCatalogBuilder()
            .Register<OutgoingMessage>("vicione.tests.inbox-outgoing-metadata")
            .Build();
        var key = new ReliableInboxKey(Guid.NewGuid(), Guid.NewGuid());
        DateTimeOffset now = TimeProvider.System.GetUtcNow();
        ReliableInboxAcquireResult acquired = await store.AcquireAsync(
            key, now, TimeSpan.FromMinutes(1), token);
        ReliableInboxLease lease = Assert.IsType<ReliableInboxLease>(acquired.Lease);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = key.ConsumerId,
            ConsumerType = nameof(IncomingMessage),
            MessageDeliveryLimit = 1,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        var incoming = InMemoryOutboxTestContextFactory.Create(
            new IncomingMessage(), token, messageId: key.MessageId, serviceProvider: services);
        var inbox = new InMemoryReliableInboxContext<ITestBus, IncomingMessage>(
            incoming, options, services, store, catalog, new DurableSendStoreLimits(10, 65536),
            key, lease, receiveCount: 1, TimeProvider.System);
        var outgoing = new MessageSendContext<OutgoingMessage>(new OutgoingMessage())
        {
            MessageId = Guid.NewGuid(),
            DestinationAddress = new Uri("loopback://inmemory-reliable-inbox/original"),
        };
        Uri originalDestination = outgoing.DestinationAddress!;
        Uri replacementDestination = new("loopback://inmemory-reliable-inbox/replacement");
        var serializer = new DestinationChangingBoundedSerializer(() =>
            outgoing.DestinationAddress = replacementDestination);
        outgoing.Serializer = serializer;
        serializer.Arm();

        MessageException failure = await Assert.ThrowsAsync<MessageException>(
            () => inbox.AddSendAsync(outgoing, token));

        Assert.Contains("DestinationAddress", failure.Message, StringComparison.Ordinal);
        Assert.True(serializer.GetterCallsAfterArming > 0);
        Assert.Equal(originalDestination, outgoing.DestinationAddress);
        await inbox.SetConsumedAsync(token);
        Assert.Equal(0, (await store.GetSnapshotAsync(token)).StoredCount);
        Assert.Empty(await store.ClaimDueAsync(now.AddSeconds(1), 10, TimeSpan.FromMinutes(1), token));
    }

    private sealed class MetadataMutationHeader(Action onRead)
    {
        public string Value
        {
            get
            {
                onRead();
                return "stable";
            }
        }
    }

    private sealed class DestinationChangingBoundedSerializer(Action changeDestination) : IBoundedMessageSerializer
    {
        private readonly ContentType _contentType = new("application/octet-stream");
        private bool _armed;

        public int GetterCallsAfterArming { get; private set; }

        public ContentType ContentType
        {
            get
            {
                if (_armed)
                {
                    GetterCallsAfterArming++;
                    changeDestination();
                }
                return _contentType;
            }
        }

        public SerializedTransportTextFormat TransportTextFormat => SerializedTransportTextFormat.Base64;

        public void Arm() => _armed = true;

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class =>
            throw new NotSupportedException("The bounded serializer path must be used.");

        public void WriteSerializedBody<T>(SendContext<T> context, IBufferWriter<byte> writer) where T : class
        {
            writer.GetSpan(1)[0] = 42;
            writer.Advance(1);
        }

        public void WriteTransportEnvelope<T>(SendContext<T> context, Stream serializedBody, IBufferWriter<byte> writer)
            where T : class
        {
            int value = serializedBody.ReadByte();
            if (value < 0)
                throw new InvalidOperationException("The admitted application body was empty.");
            writer.GetSpan(1)[0] = (byte)value;
            writer.Advance(1);
        }

        public bool TryLocateSerializedBody(ReadOnlySpan<byte> serializedEnvelope, out int offset, out int length)
        {
            offset = 0;
            length = serializedEnvelope.Length;
            return length == 1;
        }
    }

    public sealed record IncomingMessage;
    public sealed record OutgoingMessage;

    private interface ITestBus : IBus;
}
