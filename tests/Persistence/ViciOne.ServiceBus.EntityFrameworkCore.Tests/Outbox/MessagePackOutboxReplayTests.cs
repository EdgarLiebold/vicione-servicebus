using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.MessagePack;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.Outbox;

public sealed class MessagePackOutboxReplayTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BODY-CROSS-OWNER", "ef-outbox-messagepack-store-replay")]
    public async Task Outbox_StoresCanonicalMessagePackCarrierAndReplaysTheTypedEnvelopeAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        DbContextOptions<MessagePackOutboxDbContext> options =
            new DbContextOptionsBuilder<MessagePackOutboxDbContext>()
                .UseSqlite(connection)
                .Options;
        await using var dbContext = new MessagePackOutboxDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        var factory = new MessagePackSerializerFactory();
        var serializationConfiguration = new SerializationConfiguration();
        serializationConfiguration.Clear();
        serializationConfiguration.AddSerializer(factory);
        serializationConfiguration.AddDeserializer(factory, isDefault: true);
        ISerialization serialization = serializationConfiguration.CreateSerializerCollection();
        var destination = new Uri("loopback://localhost/messagepack-outbox-destination");
        var expected = new MessagePackOutboxPayload(
            Guid.Parse("3fb246c5-ff88-478d-9246-f1f2840f8fbd"),
            "entity-framework-outbox",
            [0x00, 0x80, 0xff, 0x7f, 0xfd, 0x01]);
        Guid messageId = Guid.Parse("77c65aa2-1392-4081-a289-f3241429bcf4");
        Guid correlationId = Guid.Parse("f34ff8a7-2e51-4c18-838e-30c08c108738");
        Guid outboxId = Guid.Parse("758982c3-1053-46fa-a3c4-df6e18e557b7");
        var sendContext = new MessageSendContext<MessagePackOutboxPayload>(expected)
        {
            MessageId = messageId,
            CorrelationId = correlationId,
            DestinationAddress = destination,
            Serializer = factory.CreateSerializer(),
            Serialization = serialization,
        };
        sendContext.Headers.Set("business-unit", "north");
        OutboxMessage created = OutboxMessageFactory.Create(
            sendContext,
            ServiceBusMetadataJson.ObjectDeserializer,
            TimeProvider.System,
            outboxId: outboxId);

        dbContext.Add(new OutboxState
        {
            OutboxId = outboxId,
            BusKey = "default",
            Created = DateTimeOffset.UtcNow,
            Status = OutboxDeliveryStatus.Pending,
        });
        dbContext.Add(created);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        dbContext.ChangeTracker.Clear();

        OutboxMessage stored = await dbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(TestContext.Current.CancellationToken);
        byte[] persistedBytes = Convert.FromBase64String(stored.Body);
        Assert.NotEmpty(persistedBytes);
        Assert.Equal(Convert.ToBase64String(persistedBytes), stored.Body);
        Assert.DoesNotContain(stored.Body, char.IsWhiteSpace);
        Assert.Equal(factory.ContentType.ToString(), stored.ContentType);
        Assert.Equal(messageId, stored.MessageId);
        Assert.Equal(correlationId, stored.CorrelationId);
        Assert.Equal(destination, stored.DestinationAddress);

        stored.Deserialize(ServiceBusMetadataJson.ObjectDeserializer);
        var replayContext = new MessageSendContext<SerializedTransportMessage>(SerializedTransportMessage.Instance)
        {
            Serialization = serialization,
        };
        var replayPipe = new OutboxMessageSendPipe(stored, destination);

        await replayPipe.SendAsync(replayContext);

        Assert.Equal(messageId, replayContext.MessageId);
        Assert.Equal(correlationId, replayContext.CorrelationId);
        Assert.Equal("north", replayContext.Headers.Get<string>("business-unit"));
        Assert.Equal(factory.ContentType.MediaType, replayContext.ContentType?.MediaType);
        MessageBody replayBody = replayContext.Serializer.GetMessageBody(replayContext);
        byte[] firstReplayBytes = replayBody.ToArray();
        byte[] secondReplayBytes = replayBody.ToArray();
        Assert.NotEmpty(firstReplayBytes);
        Assert.Equal(firstReplayBytes.LongLength, replayBody.Length);
        Assert.NotSame(firstReplayBytes, secondReplayBytes);
        Assert.Equal(firstReplayBytes, secondReplayBytes);
        Assert.True(replayBody.TryGetTransportText(out string? replayTransportText));
        Assert.Equal(firstReplayBytes, Convert.FromBase64String(replayTransportText));

        SerializerContext serializerContext = factory.CreateDeserializer().Deserialize(
            replayBody,
            new DictionarySendHeaders(),
            destination);
        Assert.True(serializerContext.TryGetMessage<MessagePackOutboxPayload>(out var restored));
        Assert.NotNull(restored);
        Assert.Equal(expected.CorrelationId, restored.CorrelationId);
        Assert.Equal(expected.Value, restored.Value);
        Assert.Equal(expected.Binary, restored.Binary);
        firstReplayBytes[0] ^= 0xff;
        Assert.Equal(secondReplayBytes, replayBody.ToArray());
    }

    private sealed record MessagePackOutboxPayload(Guid CorrelationId, string Value, byte[] Binary);

    private sealed class MessagePackOutboxDbContext(DbContextOptions<MessagePackOutboxDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddTransactionalOutboxEntities();
        }
    }
}
