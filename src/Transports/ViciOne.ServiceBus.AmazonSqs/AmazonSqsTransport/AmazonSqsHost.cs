using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs host implementation.
/// </summary>
public class AmazonSqsHost :
    BaseHost,
    IAmazonSqsHost
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="busTopology">The bus topology value.</param>
    public AmazonSqsHost(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>
    /// Gets the topology value.
    /// </summary>
    public new IAmazonSqsBusTopology Topology { get; }

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
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public override HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>
    /// Connects receive endpoint.
    /// </summary>
    /// <param name="definition">The definition value.</param>
    /// <param name="endpointNameFormatter">The endpoint name formatter value.</param>
    /// <param name="configureEndpoint">The configure endpoint value.</param>
    /// <returns>The result of the operation.</returns>
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint = null)
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
    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null)
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
            Type = "AmazonSQS",
            _hostConfiguration.Settings.Region
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return [_hostConfiguration.ConnectionContextSupervisor];
    }
}
