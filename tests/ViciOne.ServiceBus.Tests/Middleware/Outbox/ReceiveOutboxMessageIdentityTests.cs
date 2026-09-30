using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;
using ViciOne.ServiceBus.Middleware.Outbox.InMemory;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware.Outbox;

public sealed class ReceiveOutboxMessageIdentityTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-INMEMORY-OUTBOX-CHECKPOINT", "serializer-cannot-change-outgoing-message-identity")]
    public async Task AddSend_RejectsImmediateAndDeferredMessageIdChangesWithoutAppendingAsync(
        bool deferred, bool clearId)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();
        Guid inboxId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        ConsumeContext<Command> input = InMemoryOutboxTestContextFactory.Create(
            new Command(inboxId), token, messageId: inboxId);
        var options = new OutboxConsumeOptions
        {
            ConsumerId = consumerId,
            ConsumerType = nameof(Command),
            MessageDeliveryLimit = 10,
            MessageDeliveryTimeout = TimeSpan.FromMinutes(1),
        };
        using var inbox = new InMemoryInboxMessage(inboxId, consumerId);
        var context = new InMemoryOutboxConsumeContext<Command>(input, options, provider, inbox);
        Guid originalId = Guid.NewGuid();
        Guid replacementId = clearId ? Guid.Empty : Guid.NewGuid();
        var serializer = new MessageIdChangingSerializer(replacementId, deferred);
        var outgoing = new MessageSendContext<Command>(new Command(originalId))
        {
            MessageId = originalId,
            SupportedMessageTypes = ["urn:message:tests:Command"],
            Serializer = serializer,
        };

        MessageException failure = await Assert.ThrowsAsync<MessageException>(() => context.AddSendAsync(outgoing, token));

        Assert.Contains("changed during serialization", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(replacementId, outgoing.MessageId);
        Assert.Equal(1, serializer.SerializationCalls);
        Assert.Empty(inbox.GetOutboxMessages());
        Assert.Null(inbox.Consumed);

        Guid validId = Guid.NewGuid();
        var valid = new MessageSendContext<Command>(new Command(validId))
        {
            MessageId = validId,
            SupportedMessageTypes = ["urn:message:tests:Command"],
            Serializer = new MessageIdChangingSerializer(validId, deferred: false),
        };
        await context.AddSendAsync(valid, token);

        InMemoryOutboxMessage stored = Assert.Single(inbox.GetOutboxMessages());
        Assert.Equal(validId, stored.MessageId);
        Assert.Equal("{}", stored.Body);
        Assert.Null(inbox.Consumed);
    }

    public sealed record Command(Guid Id);

    private sealed class MessageIdChangingSerializer(Guid replacementId, bool deferred) : IMessageSerializer
    {
        public ContentType ContentType { get; } = new("application/json");

        public int SerializationCalls { get; private set; }

        public MessageBody GetMessageBody<T>(SendContext<T> context) where T : class
        {
            if (deferred)
                return new DeferredIdentityBody(() =>
                {
                    SerializationCalls++;
                    context.MessageId = replacementId;
                });

            SerializationCalls++;
            context.MessageId = replacementId;
            return new StringMessageBody("{}");
        }
    }

    private sealed class DeferredIdentityBody(Action onMaterialize) : MessageBody
    {
        public long Length => 2;

        public byte[] ToArray()
        {
            onMaterialize();
            return "{}"u8.ToArray();
        }

        public Stream OpenReadStream()
        {
            onMaterialize();
            return new MemoryStream("{}"u8.ToArray(), writable: false);
        }

        public bool TryGetTransportText([NotNullWhen(true)] out string? text)
        {
            onMaterialize();
            text = "{}";
            return true;
        }
    }
}
