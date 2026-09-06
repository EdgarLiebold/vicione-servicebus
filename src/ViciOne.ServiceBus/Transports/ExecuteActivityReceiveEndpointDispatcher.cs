namespace ViciOne.ServiceBus.Transports;

/// <summary>Dispatches execute activity receive endpoint operations.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class ExecuteActivityReceiveEndpointDispatcher<TActivity, TArguments> :
    ITypeReceiveEndpointDispatcherFactory
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The newly created instance.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        var queueName = formatter.ExecuteActivity<TActivity, TArguments>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            configurator.ConfigureExecuteActivity(registration, typeof(TActivity)));
    }
}
