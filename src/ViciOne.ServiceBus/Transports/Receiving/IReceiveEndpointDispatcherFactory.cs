using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates and owns cached receive endpoint dispatchers.</summary>
public interface IReceiveEndpointDispatcherFactory :
    IAsyncDisposable
{
    /// <summary>
    /// Creates a single receiver with all configured consumers, sagas, etc.
    /// Note that if any other receivers are created for specific consumers or sagas, those consumers and sagas will
    /// not be included in this receiver as they've already been configured.
    /// </summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <returns>The dispatcher cached for the queue.</returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName);

    /// <summary>Creates a receiver and applies capability-owned endpoint configuration.</summary>
    /// <param name="queueName">The transport queue name.</param>
    /// <param name="configure">The callback that adds registration-aware endpoint configuration.</param>
    /// <returns>The dispatcher cached for the queue.</returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName,
        Action<IReceiveEndpointConfigurator, IRegistrationContext> configure);

    /// <summary>Creates a dispatcher for a registered handler type using its owning consumer kind.</summary>
    /// <param name="registrationType">The registered consumer, saga, or activity type.</param>
    /// <param name="fallbackQueueName">The queue used when no specialized consumer kind accepts the type.</param>
    /// <param name="formatter">The formatter used to derive specialized endpoint names.</param>
    /// <returns>The dispatcher owned by the accepted consumer kind or fallback queue.</returns>
    IReceiveEndpointDispatcher CreateRegistrationReceiver(Type registrationType, string fallbackQueueName,
        IEndpointNameFormatter formatter);

}
