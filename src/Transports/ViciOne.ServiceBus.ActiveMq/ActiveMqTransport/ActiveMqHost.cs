using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq host implementation.
/// </summary>
public class ActiveMqHost :
    BaseHost,
    IActiveMqHost
{
    readonly IActiveMqHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busTopology">The bus topology value.</param>
    public ActiveMqHost(IActiveMqHostConfiguration hostConfiguration, IActiveMqBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IActiveMqBusTopology Topology { get; }

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
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint = null)
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
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IActiveMqReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "ActiveMQ",
            _hostConfiguration.Settings.Host,
            _hostConfiguration.Settings.Port,
            _hostConfiguration.Settings.Username,
            Password = new string('*', _hostConfiguration.Settings.Password.Length)
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return new IAgent[] { _hostConfiguration.ConnectionContextSupervisor };
    }
}
