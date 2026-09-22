using System.Reflection;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusReceiveTransportPropertiesTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "received-routing-metadata-survives-persisted-replay")]
    public void ReceivedRoutingMetadata_SurvivesPersistedReplay()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"),
            partitionKey: "session-17",
            sessionId: "session-17",
            replyToSessionId: "reply-session-23",
            replyTo: "reply-queue-29",
            subject: "order-created",
            properties: new Dictionary<string, object>
            {
                [AzureServiceBusTransportPropertyNames.ReplyTo] = "forged-reply",
            });
        using var receive = new ServiceBusReceiveContext(message, CreateEndpoint());

        IDictionary<string, object> properties = Assert.IsAssignableFrom<IDictionary<string, object>>(
            receive.GetTransportProperties());

        Assert.Equal(5, properties.Count);
        Assert.Equal("session-17", properties[AzureServiceBusTransportPropertyNames.PartitionKey]);
        Assert.Equal("session-17", properties[AzureServiceBusTransportPropertyNames.SessionId]);
        Assert.Equal("reply-session-23", properties[AzureServiceBusTransportPropertyNames.ReplyToSessionId]);
        Assert.Equal("reply-queue-29", properties[AzureServiceBusTransportPropertyNames.ReplyTo]);
        Assert.Equal("order-created", properties[AzureServiceBusTransportPropertyNames.Label]);

        var replay = new AzureServiceBusSendContext<MetadataMessage>(new MetadataMessage("payload"), CancellationToken.None);
        replay.ReadPropertiesFrom(new Dictionary<string, object>(properties));
        var sent = new Dictionary<string, object>();
        replay.WritePropertiesTo(sent);

        Assert.Equal(5, sent.Keys.Count(key => key.StartsWith("ASB-", StringComparison.Ordinal)));
        Assert.Equal("session-17", replay.PartitionKey);
        Assert.Equal("session-17", replay.SessionId);
        Assert.Equal("reply-session-23", replay.ReplyToSessionId);
        Assert.Equal("reply-queue-29", replay.ReplyTo);
        Assert.Equal("order-created", replay.Label);
        Assert.Equal("session-17", sent[AzureServiceBusTransportPropertyNames.PartitionKey]);
        Assert.Equal("session-17", sent[AzureServiceBusTransportPropertyNames.SessionId]);
        Assert.Equal("reply-session-23", sent[AzureServiceBusTransportPropertyNames.ReplyToSessionId]);
        Assert.Equal("reply-queue-29", sent[AzureServiceBusTransportPropertyNames.ReplyTo]);
        Assert.Equal("order-created", sent[AzureServiceBusTransportPropertyNames.Label]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "reply-destination-alone-is-persisted")]
    public void ReplyDestinationAlone_ProducesTransportProperties()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"), replyTo: "reply-only");
        using var receive = new ServiceBusReceiveContext(message, CreateEndpoint());

        IDictionary<string, object> properties = Assert.IsAssignableFrom<IDictionary<string, object>>(
            receive.GetTransportProperties());

        Assert.Single(properties);
        Assert.Equal("reply-only", properties[AzureServiceBusTransportPropertyNames.ReplyTo]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TRANSPORT-METADATA", "blank-received-routing-metadata-is-omitted")]
    public void BlankRoutingMetadata_DoesNotCreateTransportProperties()
    {
        ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString("{}"), partitionKey: " ", sessionId: " ",
            replyToSessionId: " ", replyTo: " ", subject: " ");
        using var receive = new ServiceBusReceiveContext(message, CreateEndpoint());

        Assert.Null(receive.GetTransportProperties());
    }

    static ReceiveEndpointContext CreateEndpoint() => DispatchProxy.Create<ReceiveEndpointContext, EndpointStub>();

    sealed record MetadataMessage(string Value);

    class EndpointStub : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_InputAddress" => new Uri("sb://unit.servicebus.invalid/input"),
            nameof(ReceiveEndpointContext.TryGetPayload) => MissingPayload(args),
            _ => throw new NotSupportedException($"Unexpected receive endpoint member: {targetMethod?.Name}"),
        };

        static bool MissingPayload(object?[]? args)
        {
            args![0] = null;
            return false;
        }
    }
}
