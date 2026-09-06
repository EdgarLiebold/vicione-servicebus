using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides extension methods for observer.</summary>
public static class ObserverExtensions
{
    /// <summary>Subscribes an observer instance to the bus.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">
    /// Service Bus Service Configurator
    /// - the item that is passed as a parameter to
    /// the action that is calling the configurator.
    /// </param>
    /// <param name="observer">The observer to connect to the endpoint.</param>
    /// <param name="configureCallback">The configure callback.</param>
    public static void Observer<T>(this IReceiveEndpointConfigurator configurator, IObserver<ConsumeContext<T>> observer,
        Action<IObserverConfigurator<T>>? configureCallback = null)
        where T : class
    {
        var observerConfigurator = new ObserverConfigurator<T>(observer);

        configureCallback?.Invoke(observerConfigurator);

        configurator.AddEndpointSpecification(observerConfigurator);
    }

    /// <summary>Adds a message observer to the service bus for handling a specific type of message.</summary>
    /// <typeparam name="T">The message type to handle, often inferred from the callback specified.</typeparam>
    /// <param name="bus">The bus.</param>
    /// <param name="observer">The callback to invoke when messages of the specified type arrive on the service bus.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectObserver<T>(this IBus bus, IObserver<ConsumeContext<T>> observer)
        where T : class
    {
        return ObserverConnectorCache<T>.Connector.ConnectObserver(bus, observer);
    }

    /// <summary>Subscribe a request observer to the bus's endpoint.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="bus">The bus.</param>
    /// <param name="requestId">The request id.</param>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public static ConnectHandle ConnectRequestObserver<T>(this IBus bus, Guid requestId, IObserver<ConsumeContext<T>> observer)
        where T : class
    {
        return ObserverConnectorCache<T>.Connector.ConnectRequestObserver(bus, requestId, observer);
    }
}
