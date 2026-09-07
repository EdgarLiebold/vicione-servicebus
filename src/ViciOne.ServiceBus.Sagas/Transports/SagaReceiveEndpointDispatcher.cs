namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches saga receive endpoint operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SagaReceiveEndpointDispatcher<T> :
    ITypeReceiveEndpointDispatcherFactory
    where T : class, ISaga
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The newly created instance.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        var queueName = formatter.Saga<T>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            registration.ConfigureSaga<T>(configurator));
    }
}
