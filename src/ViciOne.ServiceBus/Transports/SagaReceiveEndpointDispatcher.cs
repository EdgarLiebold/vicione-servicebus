namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a saga receive endpoint dispatcher implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SagaReceiveEndpointDispatcher<T> :
    ITypeReceiveEndpointDispatcherFactory
    where T : class, ISaga
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        var queueName = formatter.Saga<T>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            registration.ConfigureSaga<T>(configurator));
    }
}
