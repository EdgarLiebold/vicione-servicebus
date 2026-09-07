using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Hosts Amazon SQS receive endpoints and their shared AWS client infrastructure.</summary>
public class AmazonSqsHost :
    BaseHost,
    IAmazonSqsHost
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    /// <summary>Creates an Amazon SQS host from its transport configuration and bus topology.</summary>
    /// <param name="hostConfiguration">The configuration used to create endpoints and access shared AWS clients.</param>
    /// <param name="busTopology">The Amazon SQS topology exposed by the host.</param>
    public AmazonSqsHost(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    /// <summary>Gets the Amazon SQS topology associated with this host.</summary>
    public new IAmazonSqsBusTopology Topology { get; }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null" /> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that configures transport-independent endpoint settings.</param>
    /// <returns>A handle for the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects a receive endpoint for an Amazon SQS queue.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="configureEndpoint">An optional callback that configures transport-independent endpoint settings.</param>
    /// <returns>A handle for the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>Connects an Amazon SQS receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null" /> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that configures Amazon SQS-specific endpoint settings.</param>
    /// <returns>A handle for the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter = null,
        Action<IAmazonSqsReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Connects and starts an Amazon SQS receive endpoint for a queue.</summary>
    /// <param name="queueName">The Amazon SQS queue name.</param>
    /// <param name="configure">An optional callback that configures the Amazon SQS endpoint.</param>
    /// <returns>A handle for the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>Adds the AWS region and connection-supervisor state to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostic values.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "AmazonSQS",
            _hostConfiguration.Settings.Region
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    /// <summary>Gets the connection supervisors owned by the host.</summary>
    /// <returns>The host's supervised connection agents.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return [_hostConfiguration.ConnectionContextSupervisor];
    }
}
