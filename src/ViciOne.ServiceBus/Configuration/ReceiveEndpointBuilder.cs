namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a receive endpoint builder implementation.
/// </summary>
public class ReceiveEndpointBuilder :
    IReceiveEndpointBuilder
{
    readonly IReceiveEndpointConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    public ReceiveEndpointBuilder(IReceiveEndpointConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return ConnectConsumePipe(pipe, ConnectPipeOptions.ConfigureConsumeTopology);
    }

    /// <summary>
    /// Connects consume pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public virtual ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _configuration.ConsumePipe.ConnectConsumePipe(pipe);
    }
}
