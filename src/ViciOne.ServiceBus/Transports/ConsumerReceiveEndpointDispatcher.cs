namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a consumer receive endpoint dispatcher implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumerReceiveEndpointDispatcher<T> :
    ITypeReceiveEndpointDispatcherFactory
    where T : class, IConsumer
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        var queueName = formatter.Consumer<T>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            configurator.ConfigureConsumer<T>(registration));
    }
}
