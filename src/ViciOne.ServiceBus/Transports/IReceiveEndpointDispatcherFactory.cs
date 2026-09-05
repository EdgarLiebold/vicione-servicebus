using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for receive endpoint dispatcher factory.
/// </summary>
public interface IReceiveEndpointDispatcherFactory :
    IAsyncDisposable
{
    /// <summary>
    /// Creates a single receiver with all configured consumers, sagas, etc.
    /// Note that if any other receivers are created for specific consumers or sagas, those consumers and sagas will
    /// not be included in this receiver as they've already been configured.
    /// </summary>
    /// <param name="queueName"></param>
    /// <returns></returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName);

    /// <summary>
    /// Creates a receiver and applies capability-owned endpoint configuration.
    /// </summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">The endpoint configuration callback.</param>
    /// <returns>The configured dispatcher.</returns>
    IReceiveEndpointDispatcher CreateReceiver(string queueName,
        Action<IReceiveEndpointConfigurator, IRegistrationContext> configure);

    /// <summary>
    /// Creates a dispatcher for a registered handler type using its owning consumer kind.
    /// </summary>
    /// <param name="registrationType">The registered handler type.</param>
    /// <param name="fallbackQueueName">The queue name used when no consumer kind owns the type.</param>
    /// <param name="formatter">The endpoint-name formatter.</param>
    /// <returns>The typed registration dispatcher.</returns>
    IReceiveEndpointDispatcher CreateRegistrationReceiver(Type registrationType, string fallbackQueueName,
        IEndpointNameFormatter formatter);

}
