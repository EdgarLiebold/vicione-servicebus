namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates the dedicated receive dispatcher for one Courier execution activity.</summary>
/// <typeparam name="TActivity">The activity registered on the endpoint.</typeparam>
/// <typeparam name="TArguments">The activity's execution-arguments contract.</typeparam>
internal sealed class ExecuteActivityReceiveEndpointDispatcher<TActivity, TArguments> :
    ITypeReceiveEndpointDispatcherFactory
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Creates a dispatcher whose queue name and endpoint configuration belong to the activity contract.</summary>
    /// <param name="factory">The factory that owns and caches receive dispatchers.</param>
    /// <param name="formatter">The formatter used to derive the execution endpoint name.</param>
    /// <returns>The dispatcher for the activity's execution endpoint.</returns>
    public IReceiveEndpointDispatcher Create(IReceiveEndpointDispatcherFactory factory, IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(formatter);

        var queueName = formatter.ExecuteActivity<TActivity, TArguments>();

        return factory.CreateReceiver(queueName, static (configurator, registration) =>
            configurator.ConfigureExecuteActivity(registration, typeof(TActivity)));
    }
}
