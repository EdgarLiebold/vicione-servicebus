using System.Text.Json;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlTransportMessageTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HEADER-MATERIALIZATION", "serialized-header-replacement-invalidates-cache")]
    public void ReplacingSerializedHeaders_InvalidatesBothMaterializedCaches()
    {
        var message = new SqlTransportMessage
        {
            Headers = HeaderJson("application", "first"),
            TransportHeaders = HeaderJson("transport", "first"),
        };

        Assert.Equal("first", message.GetHeaders().Get("application", string.Empty));
        Assert.Equal("first", message.GetTransportHeaders().Get("transport", string.Empty));

        message.Headers = HeaderJson("application", "second");
        message.TransportHeaders = HeaderJson("transport", "second");

        Assert.Equal("second", message.GetHeaders().Get("application", string.Empty));
        Assert.Equal("second", message.GetTransportHeaders().Get("transport", string.Empty));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HEADER-MATERIALIZATION", "transport-headers-win-without-duplicates")]
    public void HeaderProvider_MergesDuplicateNamesWithTransportPrecedence()
    {
        var message = new SqlTransportMessage
        {
            Headers = HeaderJson("shared", "application"),
            TransportHeaders = HeaderJson("shared", "transport"),
        };
        var provider = new SqlHeaderProvider(message);

        KeyValuePair<string, object> header = Assert.Single(provider.GetAll());

        Assert.Equal("shared", header.Key, ignoreCase: true);
        Assert.Equal("transport", Assert.IsType<string>(header.Value));
        Assert.True(provider.TryGetHeader("SHARED", out object? value));
        Assert.Equal("transport", Assert.IsType<string>(value));
        Assert.Throws<ArgumentException>(() => provider.TryGetHeader(" ", out _));
        Assert.Throws<ArgumentNullException>(() => new SqlHeaderProvider(null!));
    }

    [Fact]
    public void HeaderProvider_PreservesApplicationOnlyHeadersBesideTransportHeaders()
    {
        var message = new SqlTransportMessage
        {
            Headers = HeaderJson("application-only", "application"),
            TransportHeaders = HeaderJson("transport-only", "transport"),
        };
        var provider = new SqlHeaderProvider(message);

        AssertHeader(provider, "APPLICATION-ONLY", "application");
        AssertHeader(provider, "TRANSPORT-ONLY", "transport");
        Dictionary<string, object> merged = provider.GetAll().ToDictionary(
            header => header.Key, header => header.Value, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(2, merged.Count);
        Assert.Equal("application", merged["APPLICATION-ONLY"]);
        Assert.Equal("transport", merged["TRANSPORT-ONLY"]);
    }

    [Fact]
    public void HeaderProvider_ProjectsDistinctNativeMetadataAndDerivedRedeliveryCount()
    {
        Guid transportId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid messageId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid requestId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid correlationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        Guid conversationId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        Guid initiatorId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var message = new SqlTransportMessage
        {
            TransportMessageId = transportId,
            MessageDeliveryId = 81,
            DeliveryCount = 3,
            ContentType = "application/vnd.example+json",
            MessageType = "urn:message:Example",
            MessageId = messageId,
            RequestId = requestId,
            CorrelationId = correlationId,
            ConversationId = conversationId,
            InitiatorId = initiatorId,
            SourceAddress = new Uri("queue:source"),
            ResponseAddress = new Uri("queue:responses"),
            FaultAddress = new Uri("queue:faults"),
            RoutingKey = "orders",
            PartitionKey = "tenant-7",
        };
        var provider = new SqlHeaderProvider(message);

        AssertHeader(provider, MessageHeaders.TransportMessageId, transportId);
        AssertHeader(provider, nameof(SqlTransportMessage.MessageDeliveryId), 81L);
        AssertHeader(provider, nameof(SqlTransportMessage.DeliveryCount), 3);
        AssertHeader(provider, MessageHeaders.RedeliveryCount, 2);
        AssertHeader(provider, MessageHeaders.ContentType, "application/vnd.example+json");
        AssertHeader(provider, MessageHeaders.MessageType, "urn:message:Example");
        AssertHeader(provider, MessageHeaders.MessageId, messageId);
        AssertHeader(provider, MessageHeaders.RequestId, requestId);
        AssertHeader(provider, MessageHeaders.CorrelationId, correlationId);
        AssertHeader(provider, MessageHeaders.ConversationId, conversationId);
        AssertHeader(provider, MessageHeaders.InitiatorId, initiatorId);
        AssertHeader(provider, MessageHeaders.SourceAddress, new Uri("queue:source"));
        AssertHeader(provider, MessageHeaders.ResponseAddress, new Uri("queue:responses"));
        AssertHeader(provider, MessageHeaders.FaultAddress, new Uri("queue:faults"));
        AssertHeader(provider, nameof(SqlTransportMessage.RoutingKey), "orders");
        AssertHeader(provider, nameof(SqlTransportMessage.PartitionKey), "tenant-7");
    }

    [Fact]
    public void HeaderProvider_ExplicitRedeliveryCountOverridesNativeDeliveryCountIncludingZero()
    {
        var message = new SqlTransportMessage
        {
            DeliveryCount = 5,
            TransportHeaders = JsonSerializer.Serialize(new[]
            {
                new KeyValuePair<string, object>(MessageHeaders.RedeliveryCount, 0)
            })
        };
        var provider = new SqlHeaderProvider(message);

        AssertHeader(provider, MessageHeaders.RedeliveryCount, 0);
        AssertHeader(provider, nameof(SqlTransportMessage.DeliveryCount), 5);
    }

    [Fact]
    public void HeaderProvider_UnsetMetadataAndFirstDeliveryDoNotInventValues()
    {
        var provider = new SqlHeaderProvider(new SqlTransportMessage { DeliveryCount = 1 });

        Assert.False(provider.TryGetHeader(MessageHeaders.MessageId, out object? messageId));
        Assert.Null(messageId);
        Assert.False(provider.TryGetHeader(MessageHeaders.ContentType, out object? contentType));
        Assert.Null(contentType);
        Assert.False(provider.TryGetHeader(MessageHeaders.RedeliveryCount, out object? redeliveryCount));
        Assert.Null(redeliveryCount);
        Assert.False(provider.TryGetHeader("missing", out object? missing));
        Assert.Null(missing);
    }

    private static void AssertHeader(SqlHeaderProvider provider, string key, object expected)
    {
        Assert.True(provider.TryGetHeader(key, out object? actual), key);
        Assert.Equal(expected, actual);
    }

    private static string HeaderJson(string key, string value) =>
        JsonSerializer.Serialize(new[] { new KeyValuePair<string, object>(key, value) });
}
