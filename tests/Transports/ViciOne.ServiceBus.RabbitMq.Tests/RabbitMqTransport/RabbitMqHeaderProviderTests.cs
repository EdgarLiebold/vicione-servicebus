using System.Collections;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqHeaderProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "absent-broker-timestamp-cannot-be-spoofed")]
    public void MissingTimestamp_CannotBeSuppliedByAnApplicationHeader()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                [MessageHeaders.TransportSentTime.ToLowerInvariant()] = "2026-09-27T12:00:00Z",
                ["neighbor"] = "retained"
            }
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("", "", "", 1, properties));

        Assert.False(properties.IsTimestampPresent());
        Assert.False(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? timestamp));
        Assert.Null(timestamp);
        Assert.DoesNotContain(provider.GetAll(), pair =>
            pair.Key.Equals(MessageHeaders.TransportSentTime, StringComparison.OrdinalIgnoreCase));
        Assert.True(provider.TryGetHeader("neighbor", out object? neighbor));
        Assert.Equal("retained", neighbor);
        Assert.Equal("2026-09-27T12:00:00Z", properties.Headers[MessageHeaders.TransportSentTime.ToLowerInvariant()]);
    }

    [Theory]
    [InlineData("value", true)]
    [InlineData("", false)]
    [InlineData(" \t", false)]
    [InlineData(null, false)]
    [InlineData(0, true)]
    [InlineData(false, true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "native-scalar-lookup-and-enumeration-agree")]
    public void ScalarLookup_AgreesWithEnumerationWithoutLosingFalsyValues(object? candidate, bool available)
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?> { ["Candidate"] = candidate, ["neighbor"] = "retained" }
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("", "", "", 1, properties));

        Assert.Equal(available, provider.TryGetHeader("cAnDiDaTe", out object? actual));
        Assert.Equal(available ? candidate : null, actual);
        Dictionary<string, object> all = provider.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value);
        Assert.Equal(available, all.ContainsKey("Candidate"));
        if (available)
            Assert.Equal(candidate, all["Candidate"]);
        Assert.Equal("retained", all["neighbor"]);
        Assert.Equal(available ? 3 : 2, all.Count);
        Assert.Equal(candidate, properties.Headers["Candidate"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "consume-context-is-required")]
    public void Constructor_RejectsAMissingConsumeContext()
    {
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new RabbitMqHeaderProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "complete-delivery-metadata-enumeration")]
    public void GetAll_ProjectsEveryAvailableDeliveryMetadataValue()
    {
        var properties = new BasicProperties
        {
            MessageId = "message-42",
            CorrelationId = "correlation-17",
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext(
            "orders", "order.created", "consumer-a", 27, properties));

        Dictionary<string, object> headers = provider.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.Equal(6, headers.Count);
        Assert.Equal("orders", headers[RabbitMqHeaders.Exchange]);
        Assert.Equal("order.created", headers[RabbitMqHeaders.RoutingKey]);
        Assert.Equal<ulong>(27, Assert.IsType<ulong>(headers[RabbitMqHeaders.DeliveryTag]));
        Assert.Equal("consumer-a", headers[RabbitMqHeaders.ConsumerTag]);
        Assert.Equal("message-42", headers[MessageHeaders.MessageId]);
        Assert.Equal("correlation-17", headers[MessageHeaders.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "native-header-normalization")]
    public void GetAll_DecodesBytesAndExcludesNullOrBlankNativeHeaders()
    {
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>
            {
                ["text-bytes"] = Encoding.UTF8.GetBytes("decoded"),
                ["blank-bytes"] = Encoding.UTF8.GetBytes("  "),
                ["text"] = "value",
                ["blank-text"] = "\t",
                ["zero"] = 0,
                ["missing"] = null,
            },
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("", "", "", 0, properties));

        Dictionary<string, object> headers = provider.GetAll().ToDictionary(pair => pair.Key, pair => pair.Value);

        Assert.Equal(4, headers.Count);
        Assert.Equal<ulong>(0, Assert.IsType<ulong>(headers[RabbitMqHeaders.DeliveryTag]));
        Assert.Equal("decoded", headers["text-bytes"]);
        Assert.Equal("value", headers["text"]);
        Assert.Equal(0, headers["zero"]);
        Assert.DoesNotContain("blank-bytes", headers.Keys);
        Assert.DoesNotContain("blank-text", headers.Keys);
        Assert.DoesNotContain("missing", headers.Keys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "case-insensitive-native-and-mapped-lookup")]
    public void TryGetHeader_LooksUpNativeAndMappedHeadersCaseInsensitively()
    {
        var properties = new BasicProperties
        {
            CorrelationId = "correlation-17",
            Headers = new Dictionary<string, object?>
            {
                ["Mixed-Case"] = Encoding.UTF8.GetBytes("decoded"),
            },
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("orders", "order.created", "consumer", 27, properties));

        Assert.True(provider.TryGetHeader("mixed-case", out object? native));
        Assert.Equal("decoded", native);
        Assert.True(provider.TryGetHeader("rabbitmq-routingkey", out object? routingKey));
        Assert.Equal("order.created", routingKey);
        Assert.True(provider.TryGetHeader("correlationid", out object? correlationId));
        Assert.Equal("correlation-17", correlationId);
        Assert.True(provider.TryGetHeader("rabbitmq-deliverytag", out object? deliveryTag));
        Assert.Equal<ulong>(27, Assert.IsType<ulong>(deliveryTag));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "empty-value-rejection")]
    public void TryGetHeader_RejectsBlankAndMissingValues()
    {
        var properties = new BasicProperties
        {
            MessageId = "  ",
            CorrelationId = "\t",
            Headers = new Dictionary<string, object?>
            {
                ["Empty"] = Encoding.UTF8.GetBytes(" "),
            },
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext(" ", "", "", 0, properties));

        Assert.False(provider.TryGetHeader("empty", out _));
        Assert.False(provider.TryGetHeader(RabbitMqHeaders.Exchange, out _));
        Assert.False(provider.TryGetHeader(RabbitMqHeaders.RoutingKey, out _));
        Assert.False(provider.TryGetHeader(RabbitMqHeaders.ConsumerTag, out _));
        Assert.False(provider.TryGetHeader(MessageHeaders.MessageId, out _));
        Assert.False(provider.TryGetHeader(MessageHeaders.CorrelationId, out _));
        Assert.False(provider.TryGetHeader("absent", out object? absent));
        Assert.Null(absent);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "broker-metadata-precedes-untrusted-native-headers")]
    public void ReservedHeaders_CannotOverrideBrokerDeliveryMetadata()
    {
        long seconds = new DateTimeOffset(2026, 9, 21, 12, 34, 56, TimeSpan.Zero).ToUnixTimeSeconds();
        var properties = new BasicProperties
        {
            MessageId = "trusted-message",
            CorrelationId = "trusted-correlation",
            Timestamp = new AmqpTimestamp(seconds),
            Headers = new Dictionary<string, object?>
            {
                [RabbitMqHeaders.Exchange.ToLowerInvariant()] = "spoof",
                [RabbitMqHeaders.RoutingKey.ToLowerInvariant()] = "spoof",
                [RabbitMqHeaders.DeliveryTag.ToLowerInvariant()] = 999UL,
                [RabbitMqHeaders.ConsumerTag.ToLowerInvariant()] = "spoof",
                [MessageHeaders.MessageId.ToLowerInvariant()] = "spoof",
                [MessageHeaders.CorrelationId.ToLowerInvariant()] = "spoof",
                [MessageHeaders.TransportSentTime.ToLowerInvariant()] = new AmqpTimestamp(0),
            },
        };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("orders", "order.created", "consumer", 27, properties));

        AssertHeader(provider, RabbitMqHeaders.Exchange, "orders");
        AssertHeader(provider, RabbitMqHeaders.RoutingKey, "order.created");
        AssertHeader(provider, RabbitMqHeaders.DeliveryTag, 27UL);
        AssertHeader(provider, RabbitMqHeaders.ConsumerTag, "consumer");
        AssertHeader(provider, MessageHeaders.MessageId, "trusted-message");
        AssertHeader(provider, MessageHeaders.CorrelationId, "trusted-correlation");
        AssertHeader(provider, MessageHeaders.TransportSentTime, DateTimeOffset.FromUnixTimeSeconds(seconds));
        Assert.DoesNotContain(provider.GetAll(), pair => Equals(pair.Value, "spoof") || Equals(pair.Value, 999UL));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "amqp-timestamp-uses-unix-seconds")]
    public void TryGetHeader_InterpretsAmqpTimestampsAsUnixSecondsAndRejectsOutOfRangeValues()
    {
        long seconds = new DateTimeOffset(2026, 9, 21, 12, 34, 56, TimeSpan.Zero).ToUnixTimeSeconds();
        var properties = new BasicProperties { Timestamp = new AmqpTimestamp(seconds) };
        var provider = new RabbitMqHeaderProvider(new ConsumeContext("exchange", "key", "consumer", 1, properties));

        Assert.True(provider.TryGetHeader("transportsenttime", out object? value));
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(seconds), Assert.IsType<DateTimeOffset>(value));

        properties.Timestamp = new AmqpTimestamp(long.MaxValue);

        Assert.False(provider.TryGetHeader(MessageHeaders.TransportSentTime, out object? outOfRange));
        Assert.Null(outOfRange);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-HEADERS", "public-timestamp-extension-reads-normalized-instant")]
    public void GetRabbitMqTimestamp_ReadsTheNormalizedProviderInstant()
    {
        DateTimeOffset expected = new(2026, 9, 21, 12, 34, 56, TimeSpan.Zero);
        var headers = new TestHeaders(new Dictionary<string, object>
        {
            [MessageHeaders.TransportSentTime] = expected,
        });
        ViciOne.ServiceBus.ConsumeContext context = DispatchProxy.Create<ViciOne.ServiceBus.ConsumeContext, ConsumeContextProxy>();
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ReceiveContextProxy>();
        ((ReceiveContextProxy)(object)receiveContext).Headers = headers;
        ((ConsumeContextProxy)(object)context).ReceiveContext = receiveContext;

        Assert.Equal(expected, context.GetRabbitMqTimestamp());
    }

    private static void AssertHeader(RabbitMqHeaderProvider provider, string key, object expected)
    {
        Assert.True(provider.TryGetHeader(key.ToLowerInvariant(), out object? actual));
        Assert.Equal(expected, actual);
    }

    private sealed record ConsumeContext(
        string Exchange,
        string? RoutingKey,
        string ConsumerTag,
        ulong DeliveryTag,
        IReadOnlyBasicProperties Properties) : RabbitMqBasicConsumeContext;

    private sealed class TestHeaders(Dictionary<string, object> values) : ViciOne.ServiceBus.Advanced.Headers
    {
        public IEnumerable<KeyValuePair<string, object>> GetAll() => values;

        public bool TryGetHeader(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? value) =>
            values.TryGetValue(key, out value);

        public TValue? Get<TValue>(string key, TValue? defaultValue = default) where TValue : class =>
            TryGetHeader(key, out object? value) ? value as TValue : defaultValue;

        public TValue? Get<TValue>(string key, TValue? defaultValue = default) where TValue : struct =>
            TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public IEnumerator<HeaderValue> GetEnumerator() => Enumerable.Empty<HeaderValue>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class ConsumeContextProxy : DispatchProxy
    {
        public ReceiveContext ReceiveContext { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_ReceiveContext" ? ReceiveContext : Default(targetMethod?.ReturnType);
    }

    private class ReceiveContextProxy : DispatchProxy
    {
        public ViciOne.ServiceBus.Advanced.Headers Headers { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.Name == "get_TransportHeaders" ? Headers : Default(targetMethod?.ReturnType);
    }

    private static object? Default(Type? type) => type?.IsValueType == true ? Activator.CreateInstance(type) : null;
}
