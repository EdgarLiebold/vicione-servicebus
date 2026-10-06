using System;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Hosts SQL-backed receive endpoints and the connection resources they share.</summary>
public class SqlHost :
    BaseHost,
    ISqlHost
{
    readonly ISqlHostConfiguration _hostConfiguration;

    /// <summary>Creates a SQL transport host from its validated configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration used to create endpoints and supervise database connections.</param>
    /// <param name="busTopology">The SQL transport topology exposed by the host.</param>
    public SqlHost(ISqlHostConfiguration hostConfiguration, ISqlBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>Gets the SQL transport topology associated with this host.</summary>
    public new ISqlBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the SQL endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<ISqlReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects a SQL-backed receive endpoint for a named queue.</summary>
    /// <param name="queueName">The logical SQL queue name.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the SQL endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<ISqlReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>Connects a SQL-backed receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies SQL-transport-specific endpoint settings.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<ISqlReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Creates, validates, builds, and starts a SQL-backed receive endpoint.</summary>
    /// <param name="queueName">The logical SQL queue name.</param>
    /// <param name="configure">An optional callback that applies SQL-transport-specific endpoint settings before validation.</param>
    /// <returns>A handle that controls the started receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<ISqlReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        try
        {
            TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);
        }
        catch (Exception)
        {
        }

        configuration.Build(this);

        return ReceiveEndpoints.Start(configuration.Settings.QueueName);
    }

    /// <summary>Adds the SQL host address and connection-supervisor state to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostic values.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "Database Transport",
            _hostConfiguration.HostAddress,
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    /// <summary>Returns the connection supervisor whose lifecycle is owned by the host.</summary>
    /// <returns>The host-owned database connection supervisor.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return new IAgent[] { _hostConfiguration.ConnectionContextSupervisor };
    }
}
