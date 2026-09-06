using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates supervised Azure Service Bus subscription processor contexts.</summary>
public class SubscriptionClientContextFactory :
    ClientContextFactory
{
    readonly SubscriptionSettings _settings;

    /// <summary>Creates a factory for one topic subscription endpoint.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="settings">The subscription entity and processor settings.</param>
    public SubscriptionClientContextFactory(IConnectionContextSupervisor supervisor, SubscriptionSettings settings)
        : base(supervisor, settings)
    {
        _settings = settings;
    }

    /// <summary>Creates a subscription processor context on an active namespace connection.</summary>
    /// <param name="connectionContext">The active namespace connection.</param>
    /// <param name="inputAddress">The absolute subscription input address.</param>
    /// <param name="agent">The agent notified when the processor faults.</param>
    /// <returns>The subscription processor context.</returns>
    protected override ClientContext CreateClientContext(ConnectionContext connectionContext, Uri inputAddress, IAgent agent)
    {
        return new SubscriptionClientContext(connectionContext, inputAddress, _settings, agent);
    }
}
