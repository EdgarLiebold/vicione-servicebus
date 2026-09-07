using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Hosts RabbitMQ receive endpoints and the connection resources they share.</summary>
public class RabbitMqHost :
    BaseHost,
    IRabbitMqHost
{
    readonly IRabbitMqHostConfiguration _hostConfiguration;

    /// <summary>Creates a RabbitMQ host from its validated transport configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration used to create endpoints and supervise the broker connection.</param>
    /// <param name="busTopology">The RabbitMQ topology exposed by the host.</param>
    public RabbitMqHost(IRabbitMqHostConfiguration hostConfiguration, IRabbitMqBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>Gets the RabbitMQ topology associated with this host.</summary>
    public new IRabbitMqBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the RabbitMQ endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IRabbitMqReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects a RabbitMQ receive endpoint for a named queue.</summary>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the RabbitMQ endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IRabbitMqReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>Connects a RabbitMQ receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies RabbitMQ-specific endpoint settings.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates, validates, builds, and starts a RabbitMQ receive endpoint.</summary>
    /// <param name="queueName">The RabbitMQ queue name.</param>
    /// <param name="configure">An optional callback that applies RabbitMQ-specific endpoint settings before validation.</param>
    /// <returns>A handle that controls the started receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>Adds masked RabbitMQ connection settings and connection-supervisor state to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostic values.</param>
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

    /// <summary>Returns the connection supervisor whose lifecycle is owned by the host.</summary>
    /// <returns>The host-owned connection supervisor.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return new IAgent[] { _hostConfiguration.ConnectionContextSupervisor };
    }
}
