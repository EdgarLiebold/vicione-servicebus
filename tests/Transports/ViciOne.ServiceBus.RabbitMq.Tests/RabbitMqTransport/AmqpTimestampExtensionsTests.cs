using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class AmqpTimestampExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TIMESTAMP", "amqp-wire-value-uses-unix-seconds")]
    public void TimestampAtOrAfterEpoch_UsesUnixSeconds()
    {
        DateTimeOffset timestamp = new(2026, 9, 4, 12, 34, 56, TimeSpan.FromHours(2));
        var headers = new Dictionary<string, object?>();

        headers.SetAmqpTimestamp("timestamp", timestamp);

        AmqpTimestamp actual = Assert.IsType<AmqpTimestamp>(headers["timestamp"]);
        Assert.Equal(timestamp.ToUnixTimeSeconds(), actual.UnixTime);
        Assert.NotEqual(timestamp.ToUnixTimeMilliseconds(), actual.UnixTime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-TIMESTAMP", "pre-epoch-value-preserves-roundtrip-text")]
    public void TimestampBeforeEpoch_PreservesTheRoundTripValueAsText()
    {
        DateTimeOffset timestamp = DateTimeOffset.UnixEpoch.AddTicks(-1);
        var headers = new Dictionary<string, object?>();

        headers.SetAmqpTimestamp("timestamp", timestamp);

        Assert.Equal(timestamp.ToString("O"), Assert.IsType<string>(headers["timestamp"]));
    }
}
