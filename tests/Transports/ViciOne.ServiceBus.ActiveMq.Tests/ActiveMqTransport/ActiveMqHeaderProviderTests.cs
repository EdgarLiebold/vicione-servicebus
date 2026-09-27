using Apache.NMS.ActiveMQ.Commands;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqHeaderProviderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "native-identities-precede-conflicting-message-properties")]
    public void IdentityLookup_UsesNativeFieldsEvenWhenApplicationPropertiesConflict(bool nativePresent)
    {
        var message = new ActiveMQMessage();
        if (nativePresent)
        {
            message.MessageId = new MessageId
            {
                ProducerId = new ProducerId { ConnectionId = "broker", SessionId = 1, Value = 2 },
                ProducerSequenceId = 3
            };
            message.NMSCorrelationID = "native-correlation";
        }
        message.Properties[MessageHeaders.TransportMessageId] = "spoof-message";
        message.Properties[MessageHeaders.CorrelationId] = "spoof-correlation";
        message.Properties["neighbor"] = 17;
        var provider = new ActiveMqHeaderProvider(message);

        // OpenWire exposes an unset native message identifier as an empty, non-null string.
        Assert.True(provider.TryGetHeader(MessageHeaders.TransportMessageId.ToLowerInvariant(), out object? id));
        Assert.Equal(nativePresent ? "broker:1:2:3" : "", id);
        Assert.Equal(nativePresent, provider.TryGetHeader(MessageHeaders.CorrelationId.ToLowerInvariant(), out object? correlation));
        Assert.Equal(nativePresent ? "native-correlation" : null, correlation);
        Assert.True(provider.TryGetHeader("neighbor", out object? neighbor));
        Assert.Equal(17, Assert.IsType<int>(neighbor));
        Assert.Equal("spoof-message", message.Properties[MessageHeaders.TransportMessageId]);
        Assert.Equal("spoof-correlation", message.Properties[MessageHeaders.CorrelationId]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "native-timestamp-retains-utc-milliseconds")]
    public void TimestampLookup_PreservesTheNativeUtcInstantAndMilliseconds(bool amqp)
    {
        DateTime expected = new(2026, 9, 27, 12, 34, 56, 789, DateTimeKind.Utc);
        Apache.NMS.IMessage message = CreateTimestampMessage(amqp, expected);
        message.Properties[MessageHeaders.TransportSentTime] = "spoof-time";
        var provider = new ActiveMqHeaderProvider(message);

        Assert.True(provider.TryGetHeader(MessageHeaders.TransportSentTime.ToLowerInvariant(), out object? timestamp));
        DateTimeOffset actual = Assert.IsType<DateTimeOffset>(timestamp);
        Assert.Equal(new DateTimeOffset(expected), actual);
        Assert.Equal(TimeSpan.Zero, actual.Offset);
        Assert.Equal(789, actual.Millisecond);
        Assert.Equal("spoof-time", message.Properties[MessageHeaders.TransportSentTime]);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, -1)]
    [InlineData(true, 0)]
    [InlineData(true, -1)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "timestamp-eligibility-compares-normalized-utc-to-epoch")]
    public void TimestampAtOrBeforeEpoch_IsUnavailableRegardlessOfLocalOffset(bool amqp, long milliseconds)
    {
        Apache.NMS.IMessage message = CreateTimestampMessage(amqp, DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime);
        message.Properties["neighbor"] = "retained";
        var provider = new ActiveMqHeaderProvider(message);

        Assert.False(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? timestamp));
        Assert.Null(timestamp);
        Assert.True(provider.TryGetHeader("neighbor", out object? neighbor));
        Assert.Equal("retained", neighbor);
    }

    private static Apache.NMS.IMessage CreateTimestampMessage(bool amqp, DateTime timestamp)
    {
        if (!amqp)
            return new ActiveMQMessage { NMSTimestamp = timestamp };

        var facade = new Apache.NMS.AMQP.Provider.Amqp.Message.AmqpNmsMessageFacade();
        // Native message properties require no connection or broker operations.
        facade.Initialize(null!);
        return new Apache.NMS.AMQP.Message.NmsMessage(facade) { NMSTimestamp = timestamp };
    }
}
