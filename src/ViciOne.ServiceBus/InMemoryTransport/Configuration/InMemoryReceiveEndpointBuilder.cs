using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Builds in memory receive endpoint components.</summary>
public class InMemoryReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IInMemoryReceiveEndpointConfiguration _configuration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="hostConfiguration">The host configuration.</param>
    /// <param name="configuration">The callback used to configure the component.</param>
    public InMemoryReceiveEndpointBuilder(IInMemoryHostConfiguration hostConfiguration, IInMemoryReceiveEndpointConfiguration configuration)
        : base(configuration)
    {
        _hostConfiguration = hostConfiguration;
        _configuration = configuration;
    }

    /// <summary>Connects consume pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public override ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
    {
        if (_configuration.ConfigureConsumeTopology && options.HasFlag(ConnectPipeOptions.ConfigureConsumeTopology))
        {
            IInMemoryMessageConsumeTopologyConfigurator<T> topology = _configuration.Topology.Consume.GetMessageTopology<T>();
            if (topology.ConfigureConsumeTopology)
                topology.Bind();
        }

        return base.ConnectConsumePipe(pipe, options);
    }

    /// <summary>Creates receive endpoint context.</summary>
    /// <returns>The created receive endpoint context.</returns>
    public InMemoryReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var context = new TransportInMemoryReceiveEndpointContext(_hostConfiguration, _configuration);

        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }
}
