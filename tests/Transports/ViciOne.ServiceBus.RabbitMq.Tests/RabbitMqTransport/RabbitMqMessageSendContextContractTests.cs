using RabbitMQ.Client;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport;

public sealed class RabbitMqMessageSendContextContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MESSAGE-SEND-CONTEXT", "basic-properties-are-required")]
    public void Constructor_RejectsMissingBasicProperties()
    {
        Assert.Equal(
            "basicProperties",
            Assert.Throws<ArgumentNullException>(
                () => new RabbitMqMessageSendContext<Message>(null!, "orders", new Message(), CancellationToken.None)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MESSAGE-SEND-CONTEXT", "complete-transport-property-roundtrip")]
    public void TransportProperties_RoundTripEveryExplicitRabbitMqValue()
    {
        var properties = new BasicProperties
        {
            AppId = "orders-app",
            Priority = 7,
            ReplyTo = "reply.queue",
            Type = "order-created",
            UserId = "service-user",
        };
        var source = new RabbitMqMessageSendContext<Message>(properties, "orders", new Message(), CancellationToken.None)
        {
            RoutingKey = "orders.created",
        };
        var persisted = new Dictionary<string, object>();

        source.WritePropertiesTo(persisted);
        var restored = new RabbitMqMessageSendContext<Message>(new BasicProperties(), "fallback", new Message(), CancellationToken.None);
        restored.ReadPropertiesFrom(persisted);

        Assert.Equal(7, persisted.Count);
        Assert.Equal("orders", restored.Exchange);
        Assert.Equal("orders.created", restored.RoutingKey);
        Assert.Equal("orders-app", restored.BasicProperties.AppId);
        Assert.Equal<byte>(7, restored.BasicProperties.Priority);
        Assert.Equal("reply.queue", restored.BasicProperties.ReplyTo);
        Assert.Equal("order-created", restored.BasicProperties.Type);
        Assert.Equal("service-user", restored.BasicProperties.UserId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MESSAGE-SEND-CONTEXT", "default-priority-roundtrip")]
    public void DefaultPriority_RemainsZeroWithoutAnExplicitTransportProperty()
    {
        var source = new RabbitMqMessageSendContext<Message>(new BasicProperties(), "orders", new Message(), CancellationToken.None);
        var persisted = new Dictionary<string, object>();

        source.WritePropertiesTo(persisted);
        var restored = new RabbitMqMessageSendContext<Message>(new BasicProperties(), "fallback", new Message(), CancellationToken.None);
        restored.ReadPropertiesFrom(persisted);

        Assert.DoesNotContain(RabbitMqTransportPropertyNames.Priority, persisted.Keys);
        Assert.Equal<byte>(0, restored.BasicProperties.Priority);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-MESSAGE-SEND-CONTEXT", "blank-values-are-not-persisted")]
    public void WritePropertiesTo_OmitsBlankRoutingAndStringProperties()
    {
        var properties = new BasicProperties
        {
            AppId = "",
            ReplyTo = " ",
            Type = "\t",
            UserId = "\r\n",
        };
        var context = new RabbitMqMessageSendContext<Message>(properties, "orders", new Message(), CancellationToken.None)
        {
            RoutingKey = " ",
        };
        var persisted = new Dictionary<string, object>();

        context.WritePropertiesTo(persisted);

        KeyValuePair<string, object> property = Assert.Single(persisted);
        Assert.Equal(RabbitMqTransportPropertyNames.Exchange, property.Key);
        Assert.Equal("orders", property.Value);
    }

    private sealed class Message;
}
