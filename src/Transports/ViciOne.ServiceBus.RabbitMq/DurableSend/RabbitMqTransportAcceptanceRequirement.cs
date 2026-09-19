using System.Threading;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Marks a send that may complete only after RabbitMQ confirms broker acceptance.</summary>
internal sealed class RabbitMqTransportAcceptanceRequirement
{
    private int _accepted;

    public RabbitMqTransportAcceptanceRequirement(string exchange, bool requiresExistingQueueProof)
    {
        Exchange = exchange;
        RequiresExistingQueueProof = requiresExistingQueueProof;
    }

    public string Exchange { get; }

    public bool RequiresExistingQueueProof { get; }

    public bool Accepted => Volatile.Read(ref _accepted) != 0;

    public void MarkAccepted() => Volatile.Write(ref _accepted, 1);
}
