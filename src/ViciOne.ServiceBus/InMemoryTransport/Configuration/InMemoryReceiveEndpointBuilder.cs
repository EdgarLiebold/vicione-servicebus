using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.InMemoryTransport.Configuration;

/// <summary>Builds an in-memory receive pipeline and its message-topology bindings.</summary>
internal sealed class InMemoryReceiveEndpointBuilder :
    ReceiveEndpointBuilder
{
    readonly IInMemoryReceiveEndpointConfiguration _configuration;
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Creates a builder for one receive endpoint configuration.</summary>
    /// <param name="hostConfiguration">The host that owns the endpoint.</param>
    /// <param name="configuration">The receive endpoint configuration to materialize.</param>
    public InMemoryReceiveEndpointBuilder(IInMemoryHostConfiguration hostConfiguration, IInMemoryReceiveEndpointConfiguration configuration)
        : base(configuration ?? throw new ArgumentNullException(nameof(configuration)))
    {
        _hostConfiguration = hostConfiguration ?? throw new ArgumentNullException(nameof(hostConfiguration));
        _configuration = configuration;
    }

    /// <summary>Connects a consume pipe and adds its message exchange binding when requested.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="pipe">The consume pipe to connect.</param>
    /// <param name="options">The options that control topology configuration.</param>
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

    /// <summary>Creates the runtime endpoint context and adds the host topology as a payload.</summary>
    /// <returns>The configured runtime endpoint context.</returns>
    public IInMemoryReceiveEndpointContext CreateReceiveEndpointContext()
    {
        var context = new InMemoryReceiveEndpointContext(_hostConfiguration, _configuration);

        context.GetOrAddPayload(() => _hostConfiguration.Topology);

        return context;
    }
}
