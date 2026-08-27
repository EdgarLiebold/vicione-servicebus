namespace ViciOne.ServiceBus.AmazonSqsTransport;

using System;
using Configuration;
using Transports;


public class AmazonSqsHost :
    BaseHost,
    IAmazonSqsHost
{
    readonly IAmazonSqsHostConfiguration _hostConfiguration;

    public AmazonSqsHost(IAmazonSqsHostConfiguration hostConfiguration, IAmazonSqsBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
        Topology = busTopology;
    }

    public new IAmazonSqsBusTopology Topology { get; }

    public override HostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    public override HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IAmazonSqsReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

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

    public HostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IAmazonSqsReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    protected override void Probe(ProbeContext context)
    {
        context.Set(new
        {
            Type = "AmazonSQS",
            _hostConfiguration.Settings.Region
        });

        _hostConfiguration.ConnectionContextSupervisor.Probe(context);
    }

    protected override IAgent[] GetAgentHandles()
    {
        return [_hostConfiguration.ConnectionContextSupervisor];
    }
}
