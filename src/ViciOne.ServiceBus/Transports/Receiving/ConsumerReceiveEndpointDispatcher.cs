namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates a dedicated receive dispatcher for a registered consumer type.</summary>
/// <typeparam name="T">The consumer type configured on the receive endpoint.</typeparam>
public sealed class ConsumerReceiveEndpointDispatcher<T> :
    ITypeReceiveEndpointDispatcherFactory
    where T : class, IConsumer
{
    /// <summary>Creates the consumer's receive endpoint dispatcher.</summary>
    /// <param name="factory">The factory that owns dispatcher instances.</param>
    /// <param name="formatter">The formatter used to derive the consumer queue name.</param>
    /// <returns>The cached dispatcher for the consumer's queue.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        var queueName = formatter.Consumer<T>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            configurator.ConfigureConsumer<T>(registration));
    }
}
