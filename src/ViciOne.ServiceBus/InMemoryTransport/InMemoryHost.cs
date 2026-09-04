using System;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Caches InMemory transport instances so that they are only created and used once
/// </summary>
public class InMemoryHost :
    BaseHost,
    IInMemoryHost
{
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busTopology">The bus topology value.</param>
    public InMemoryHost(IInMemoryHostConfiguration hostConfiguration, IInMemoryBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public override HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configureEndpoint);
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public override HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        return ConnectReceiveEndpoint(queueName, configureEndpoint);
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>
    /// Gets the delay provider value.
    /// </summary>
    public IInMemoryDelayProvider DelayProvider => _hostConfiguration.TransportProvider.MessageFabric.DelayProvider;

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Add("type", "InMemory");
        context.Add("baseAddress", _hostConfiguration.HostAddress);

        _hostConfiguration.TransportProvider.Probe(context);
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return [_hostConfiguration.TransportProvider];
    }
}
