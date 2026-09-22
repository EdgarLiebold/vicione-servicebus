using System.Text;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class AzureServiceBusTransportMetadataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "persisted-routing-metadata-roundtrips-without-losing-unrelated-properties")]
    public void PersistedRoutingMetadata_RoundTripsExactlyAndPreservesOtherProperties()
    {
        var original = CreateContext();
        original.SessionId = "session-17";
        original.ReplyToSessionId = "reply-session";
        original.ReplyTo = "reply-queue";
        original.Label = "order-created";
        var properties = new Dictionary<string, object> { ["other-provider"] = 42 };

        original.WritePropertiesTo(properties);

        Assert.Equal(5, properties.Keys.Count(key => key.StartsWith("ASB-", StringComparison.Ordinal)));
        Assert.Equal(42, properties["other-provider"]);
        Assert.Equal("session-17", properties[AzureServiceBusTransportPropertyNames.PartitionKey]);
        Assert.Equal("session-17", properties[AzureServiceBusTransportPropertyNames.SessionId]);
        Assert.Equal("reply-session", properties[AzureServiceBusTransportPropertyNames.ReplyToSessionId]);
        Assert.Equal("reply-queue", properties[AzureServiceBusTransportPropertyNames.ReplyTo]);
        Assert.Equal("order-created", properties[AzureServiceBusTransportPropertyNames.Label]);

        var restored = CreateContext();
        restored.ReadPropertiesFrom(properties);

        Assert.Equal("session-17", restored.PartitionKey);
        Assert.Equal("session-17", restored.SessionId);
        Assert.Equal("reply-session", restored.ReplyToSessionId);
        Assert.Equal("reply-queue", restored.ReplyTo);
        Assert.Equal("order-created", restored.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "utf8-routing-values-and-session-precedence")]
    public void ReadPropertiesFrom_DecodesUtf8AndMakesSessionWinOverConflictingPartition()
    {
        var context = CreateContext();
        context.ReplyToSessionId = "original-reply-session";
        context.ReplyTo = "original-reply-queue";
        var properties = new Dictionary<string, object>
        {
            [AzureServiceBusTransportPropertyNames.PartitionKey] = Encoding.UTF8.GetBytes("partition-17"),
            [AzureServiceBusTransportPropertyNames.SessionId] = Encoding.UTF8.GetBytes("session-23"),
            [AzureServiceBusTransportPropertyNames.ReplyToSessionId] = " ",
            [AzureServiceBusTransportPropertyNames.ReplyTo] = 17,
            [AzureServiceBusTransportPropertyNames.Label] = Encoding.UTF8.GetBytes("label-29"),
        };

        context.ReadPropertiesFrom(properties);

        Assert.Equal("session-23", context.SessionId);
        Assert.Equal("session-23", context.PartitionKey);
        Assert.Equal("original-reply-session", context.ReplyToSessionId);
        Assert.Equal("original-reply-queue", context.ReplyTo);
        Assert.Equal("label-29", context.Label);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "blank-metadata-is-not-persisted")]
    public void WritePropertiesTo_OmitsBlankProviderMetadata()
    {
        var context = CreateContext();
        context.SessionId = " ";
        context.ReplyToSessionId = " ";
        context.ReplyTo = " ";
        context.Label = " ";
        var properties = new Dictionary<string, object> { ["other-provider"] = 42 };

        context.WritePropertiesTo(properties);

        Assert.DoesNotContain(properties.Keys, key => key.StartsWith("ASB-", StringComparison.Ordinal));
        Assert.Equal(42, properties["other-provider"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "session-and-partition-identity-stays-consistent")]
    public void SessionAndPartitionKey_KeepOneRoutingIdentity()
    {
        var context = CreateContext();
        context.SessionId = "session-17";

        Assert.Equal("session-17", context.PartitionKey);
        context.PartitionKey = "session-17";
        Assert.Equal("session-17", context.SessionId);

        context.PartitionKey = "partition-23";
        Assert.Null(context.SessionId);
        Assert.Equal("partition-23", context.PartitionKey);

        context.SessionId = "session-29";
        Assert.Equal("session-29", context.SessionId);
        Assert.Equal("session-29", context.PartitionKey);
    }

    private static AzureServiceBusSendContext<MetadataMessage> CreateContext() =>
        new(new MetadataMessage("body"), CancellationToken.None);

    private sealed record MetadataMessage(string Value);
}
