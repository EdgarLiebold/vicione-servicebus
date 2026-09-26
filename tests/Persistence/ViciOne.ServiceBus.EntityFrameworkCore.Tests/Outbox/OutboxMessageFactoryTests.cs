using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Serialization;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class OutboxMessageFactoryTests
{
    private static readonly DateTimeOffset Now = new(2042, 3, 4, 5, 6, 7, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-requires-every-collaborator-and-message-identity")]
    public void Create_RejectsMissingCollaboratorsAndAnEmptyMessageIdentity()
    {
        MessageSendContext<FactoryMessage> context = CreateContext();
        Guid outboxId = Guid.NewGuid();

        Assert.Throws<ArgumentNullException>(() => OutboxMessageFactory.Create<FactoryMessage>(
            null!, ServiceBusMetadataJson.ObjectDeserializer, TimeProvider.System, outboxId: outboxId));
        Assert.Throws<ArgumentNullException>(() => OutboxMessageFactory.Create(
            context, null!, TimeProvider.System, outboxId: outboxId));
        Assert.Throws<ArgumentNullException>(() => OutboxMessageFactory.Create(
            context, ServiceBusMetadataJson.ObjectDeserializer, null!, outboxId: outboxId));

        context.MessageId = null;
        Assert.Throws<MessageException>(() => OutboxMessageFactory.Create(
            context, ServiceBusMetadataJson.ObjectDeserializer, TimeProvider.System, outboxId: outboxId));
        context.MessageId = Guid.Empty;
        Assert.Throws<MessageException>(() => OutboxMessageFactory.Create(
            context, ServiceBusMetadataJson.ObjectDeserializer, TimeProvider.System, outboxId: outboxId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-requires-exactly-one-complete-nonempty-owner")]
    public void Create_RejectsMissingPartialEmptyAndAmbiguousOwnership()
    {
        MessageSendContext<FactoryMessage> context = CreateContext();
        Guid messageId = Guid.NewGuid();
        Guid consumerId = Guid.NewGuid();
        Guid outboxId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => Create(context));
        Assert.Throws<ArgumentException>(() => Create(context, inboxMessageId: messageId));
        Assert.Throws<ArgumentException>(() => Create(context, inboxConsumerId: consumerId));
        Assert.Throws<ArgumentException>(() => Create(context, Guid.Empty, consumerId));
        Assert.Throws<ArgumentException>(() => Create(context, messageId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => Create(context, outboxId: Guid.Empty));
        Assert.Throws<ArgumentException>(() => Create(context, messageId, consumerId, outboxId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-preserves-complete-envelope-and-transport-metadata")]
    public void Create_WithInboxOwnerPreservesEnvelopeHeadersPropertiesAndDerivedTimes()
    {
        Guid inboxMessageId = Guid.NewGuid();
        Guid inboxConsumerId = Guid.NewGuid();
        Guid messageId = Guid.NewGuid();
        Guid conversationId = Guid.NewGuid();
        Guid correlationId = Guid.NewGuid();
        Guid initiatorId = Guid.NewGuid();
        Guid requestId = Guid.NewGuid();
        var sourceAddress = new Uri("loopback://localhost/source");
        var destinationAddress = new Uri("loopback://localhost/destination");
        var responseAddress = new Uri("loopback://localhost/response");
        var faultAddress = new Uri("loopback://localhost/fault");
        MessageSendContext<FactoryMessage> context = CreateContext();
        context.MessageId = messageId;
        context.CorrelationId = correlationId;
        context.ConversationId = conversationId;
        context.InitiatorId = initiatorId;
        context.RequestId = requestId;
        context.SourceAddress = sourceAddress;
        context.DestinationAddress = destinationAddress;
        context.ResponseAddress = responseAddress;
        context.FaultAddress = faultAddress;
        context.TimeToLive = TimeSpan.FromMinutes(5);
        context.Delay = TimeSpan.FromMinutes(2);
        context.Durable = false;
        context.Mandatory = true;
        context.Headers.Set("tenant", "north");

        OutboxMessage message = Create(context, inboxMessageId, inboxConsumerId);
        message.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Equal(messageId, message.MessageId);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(initiatorId, message.InitiatorId);
        Assert.Equal(requestId, message.RequestId);
        Assert.Equal(sourceAddress, message.SourceAddress);
        Assert.Equal(destinationAddress, message.DestinationAddress);
        Assert.Equal(responseAddress, message.ResponseAddress);
        Assert.Equal(faultAddress, message.FaultAddress);
        Assert.Equal(context.SentTime, message.SentTime);
        Assert.Equal(Now.AddMinutes(5), message.ExpirationTime);
        Assert.Equal(Now.AddMinutes(2), message.EnqueueTime);
        Assert.Equal(inboxMessageId, message.InboxMessageId);
        Assert.Equal(inboxConsumerId, message.InboxConsumerId);
        Assert.Null(message.OutboxId);
        Assert.Equal(ServiceBusMetadataJson.MessageSerializer.ContentType.ToString(), message.ContentType);
        Assert.Equal(string.Join(";", context.SupportedMessageTypes), message.MessageType);
        Assert.NotEmpty(message.Body);

        MessageContext messageContext = message;
        Assert.Equal("north", messageContext.Headers.Get<string>("tenant"));
        IReadOnlyDictionary<string, object> properties = ((OutboxMessageContext)message).Properties;
        Assert.False(Assert.IsType<bool>(properties["Durable"]));
        Assert.True(Assert.IsType<bool>(properties["Mandatory"]));
        Assert.Equal(TimeSpan.FromMinutes(2).ToString(), Assert.IsType<string>(properties["Delay"]));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-accepts-one-transactional-outbox-owner")]
    public void Create_WithTransactionalOwnerAssignsOnlyThatOwner()
    {
        Guid outboxId = Guid.NewGuid();

        OutboxMessage message = Create(CreateContext(), outboxId: outboxId);

        Assert.Equal(outboxId, message.OutboxId);
        Assert.Null(message.InboxMessageId);
        Assert.Null(message.InboxConsumerId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("body-only")]
    [InlineData("envelope-only")]
    [InlineData("different-envelope")]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-rejects-missing-incomplete-or-mismatched-admission")]
    public void Create_RejectsAnAdmittedBodyWithoutItsCompleteMatchingProof(string proofState)
    {
        MessageSendContext<FactoryMessage> context = CreateContext();
        MessageBody body = context.Serializer.GetMessageBody(context);
        byte[] bytes = body.ToArray();
        if (proofState != "missing")
        {
            byte[] validatedBytes = (byte[])bytes.Clone();
            if (proofState == "different-envelope")
                validatedBytes[^1] ^= 1;
            PayloadAdmissionSerializationTestDriver.AttachAdmissionEvidence(context,
                proofState == "envelope-only" ? null : bytes,
                proofState == "body-only" ? null : validatedBytes);
        }

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            OutboxMessageFactory.Create(context, ServiceBusMetadataJson.ObjectDeserializer,
                new FakeTimeProvider(Now), outboxId: Guid.NewGuid(), admittedBody: body));

        Assert.Equal("The EF outbox has no complete payload admission proof for its serialized envelope.", failure.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-OUTBOX-METADATA", "factory-persists-matching-admission-proof-and-application-headers")]
    public void Create_PersistsTheExactAdmittedEnvelopeAndItsProofAlongsideApplicationHeaders()
    {
        MessageSendContext<FactoryMessage> context = CreateContext();
        context.Headers.Set("tenant", "north");
        MessageBody body = context.Serializer.GetMessageBody(context);
        byte[] bytes = body.ToArray();
        using JsonDocument envelope = JsonDocument.Parse(bytes);
        byte[] applicationBody = Encoding.UTF8.GetBytes(envelope.RootElement.GetProperty("message").GetRawText());
        Assert.True(applicationBody.Length < bytes.Length);
        PayloadAdmissionSerializationTestDriver.AttachAdmissionEvidence(context, applicationBody, bytes);
        Guid outboxId = Guid.NewGuid();

        OutboxMessage stored = OutboxMessageFactory.Create(context,
            ServiceBusMetadataJson.ObjectDeserializer, new FakeTimeProvider(Now),
            outboxId: outboxId, admittedBody: body);
        stored.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);

        Assert.Equal(bytes, Encoding.UTF8.GetBytes(stored.Body));
        Assert.Equal(outboxId, stored.OutboxId);
        Assert.Equal("north", ((MessageContext)stored).Headers.Get<string>("tenant"));
        var proof = PayloadAdmissionSerializationTestDriver.ReadDurableProof(stored, bytes, stored.ContentType);
        Assert.Equal(applicationBody.Length, proof.BodyBytes);
        Assert.False(proof.OffloadObserved);
        Assert.True(proof.MatchesEnvelope);
    }

    private static MessageSendContext<FactoryMessage> CreateContext() => new(new FactoryMessage("payload"))
    {
        Serializer = ServiceBusMetadataJson.MessageSerializer,
    };

    private static OutboxMessage Create(
        MessageSendContext<FactoryMessage> context,
        Guid? inboxMessageId = null,
        Guid? inboxConsumerId = null,
        Guid? outboxId = null) =>
        OutboxMessageFactory.Create(
            context,
            ServiceBusMetadataJson.ObjectDeserializer,
            new FakeTimeProvider(Now),
            inboxMessageId,
            inboxConsumerId,
            outboxId);

    private sealed record FactoryMessage(string Value);
}
