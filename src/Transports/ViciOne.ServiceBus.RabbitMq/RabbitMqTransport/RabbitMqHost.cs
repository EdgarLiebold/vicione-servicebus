using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq host implementation.
/// </summary>
public class RabbitMqHost :
    BaseHost,
    IRabbitMqHost
{
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busTopology">The bus topology value.</param>
    public RabbitMqHost(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IRabbitMqBusTopology Topology { get; }

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
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint = null)
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
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator>? configure = null)
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
            Type = "RabbitMQ",
            _hostConfiguration.Settings.Host,
            _hostConfiguration.Settings.Port,
            _hostConfiguration.Settings.VirtualHost,
            _hostConfiguration.Settings.Username,
            Password = new string('*', _hostConfiguration.Settings.Password?.Length ?? 0),
            _hostConfiguration.Settings.Heartbeat,
            _hostConfiguration.Settings.Ssl
        });

        if (_hostConfiguration.Settings.Ssl)
            context.Set(new { _hostConfiguration.Settings.SslServerName });

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
