namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Marks a send that may complete only after RabbitMQ confirms broker acceptance.</summary>
internal sealed class RabbitMqTransportAcceptanceRequirement
{
    private RabbitMqTransportAcceptanceRequirement()
    {
    }

    public static RabbitMqTransportAcceptanceRequirement Instance { get; } = new();
}
