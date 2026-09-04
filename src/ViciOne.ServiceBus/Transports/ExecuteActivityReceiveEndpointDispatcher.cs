namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides an execute activity receive endpoint dispatcher implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityReceiveEndpointDispatcher<TActivity, TArguments> :
    ITypeReceiveEndpointDispatcherFactory
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        var queueName = formatter.ExecuteActivity<TActivity, TArguments>();

        return factory.CreateExecuteActivityReceiver<TActivity>(queueName);
    }
}
