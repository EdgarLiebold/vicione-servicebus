using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates supervised Azure Service Bus queue processor contexts.</summary>
public class QueueClientContextFactory :
    ClientContextFactory
{
    readonly ReceiveSettings _settings;

    /// <summary>Creates a factory for one queue endpoint.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="settings">The queue entity and processor settings.</param>
    public QueueClientContextFactory(IConnectionContextSupervisor supervisor, ReceiveSettings settings)
        : base(supervisor, settings)
    {
        _settings = settings;
    }

    /// <summary>Creates a queue processor context on an active namespace connection.</summary>
    /// <param name="connectionContext">The active namespace connection.</param>
    /// <param name="inputAddress">The absolute queue input address.</param>
    /// <param name="agent">The agent notified when the processor faults.</param>
    /// <returns>The queue processor context.</returns>
    protected override ClientContext CreateClientContext(ConnectionContext connectionContext, Uri inputAddress, IAgent agent)
    {
        return new QueueClientContext(connectionContext, inputAddress, _settings, agent);
    }
}
