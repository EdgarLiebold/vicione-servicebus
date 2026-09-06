namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds receive endpoint components.</summary>
public class ReceiveEndpointBuilder :
    IReceiveEndpointBuilder
{
    readonly IReceiveEndpointConfiguration _configuration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configuration">The callback used to configure the component.</param>
    public ReceiveEndpointBuilder(IReceiveEndpointConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class
    {
        return ConnectConsumePipe(pipe, ConnectPipeOptions.ConfigureConsumeTopology);
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public virtual ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
        where T : class
    {
        return _configuration.ConsumePipe.ConnectConsumePipe(pipe);
    }
}
