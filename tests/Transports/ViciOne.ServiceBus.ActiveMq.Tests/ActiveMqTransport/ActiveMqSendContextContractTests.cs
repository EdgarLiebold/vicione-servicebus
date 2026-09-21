using Apache.NMS;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ActiveMqSendContextContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-SEND-CONTEXT", "complete-transport-property-roundtrip")]
    public void TransportProperties_RoundTripEveryExplicitActiveMqValue()
    {
        var source = new TransportActiveMqSendContext<Message>(new Message(), CancellationToken.None)
        {
            Priority = MsgPriority.VeryHigh,
            GroupId = "order-27",
            GroupSequence = 3,
        };
        var persisted = new Dictionary<string, object>();

        source.WritePropertiesTo(persisted);
        var restored = new TransportActiveMqSendContext<Message>(new Message(), CancellationToken.None);
        restored.ReadPropertiesFrom(persisted);

        Assert.Equal(3, persisted.Count);
        Assert.Equal(source.Priority, restored.Priority);
        Assert.Equal("order-27", restored.GroupId);
        Assert.Equal(3, restored.GroupSequence);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-SEND-CONTEXT", "absent-values-remain-absent")]
    public void TransportProperties_OmitAndRestoreAbsentActiveMqValues()
    {
        var source = new TransportActiveMqSendContext<Message>(new Message(), CancellationToken.None);
        var persisted = new Dictionary<string, object>();

        source.WritePropertiesTo(persisted);
        source.Priority = MsgPriority.VeryHigh;
        source.GroupId = "stale";
        source.GroupSequence = 9;
        source.ReadPropertiesFrom(persisted);

        Assert.Empty(persisted);
        Assert.Null(source.Priority);
        Assert.Null(source.GroupId);
        Assert.Null(source.GroupSequence);
    }

    private sealed record Message;
}
