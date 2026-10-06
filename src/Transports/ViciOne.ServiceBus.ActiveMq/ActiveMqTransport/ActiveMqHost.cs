using System;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Hosts ActiveMQ receive endpoints and the connection resources they share.</summary>
public class ActiveMqHost :
    BaseHost,
    IActiveMqHost
{
    readonly IActiveMqHostConfiguration _hostConfiguration;

    /// <summary>Creates an ActiveMQ host from its validated transport configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration used to create endpoints and supervise the broker connection.</param>
    /// <param name="busTopology">The ActiveMQ topology exposed by the host.</param>
    public ActiveMqHost(IActiveMqHostConfiguration hostConfiguration, IActiveMqBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>Gets the ActiveMQ topology associated with this host.</summary>
    public new IActiveMqBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the ActiveMQ endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IActiveMqReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects an ActiveMQ receive endpoint for a named queue.</summary>
    /// <param name="queueName">The ActiveMQ queue name.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the ActiveMQ endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IActiveMqReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>Connects an ActiveMQ receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies ActiveMQ-specific endpoint settings.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IActiveMqReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates, validates, builds, and starts an ActiveMQ receive endpoint.</summary>
    /// <param name="queueName">The ActiveMQ queue name.</param>
    /// <param name="configure">An optional callback that applies ActiveMQ-specific endpoint settings before validation.</param>
    /// <returns>A handle that controls the started receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IActiveMqReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        try
        {
            TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);
        }
        catch
        {
            // Optional diagnostics must not interrupt transport creation or topology setup.
        }

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>Adds the non-secret ActiveMQ connection settings and connection-supervisor state to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostic values.</param>
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

    /// <summary>Returns the connection supervisor whose lifecycle is owned by the host.</summary>
    /// <returns>The host-owned connection supervisor.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return new IAgent[] { _hostConfiguration.ConnectionContextSupervisor };
    }
}
