using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a queue client context factory implementation.
/// </summary>
public class QueueClientContextFactory :
    ClientContextFactory
{
    readonly ReceiveSettings _settings;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="settings">The settings value.</param>
    public QueueClientContextFactory(IConnectionContextSupervisor supervisor, ReceiveSettings settings)
        : base(supervisor, settings)
    {
        _settings = settings;
    }

    /// <summary>
    /// Creates client context.
    /// </summary>
    /// <param name="connectionContext">The connection context value.</param>
    /// <param name="inputAddress">The input address value.</param>
    /// <param name="agent">The agent value.</param>
    /// <returns>The result of the operation.</returns>
    protected override ClientContext CreateClientContext(ConnectionContext connectionContext, Uri inputAddress, IAgent agent)
    {
        return new QueueClientContext(connectionContext, inputAddress, _settings, agent);
    }
}
