using System;
using ViciOne.ServiceBus.InMemoryTransport.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>Hosts receive endpoints on a shared in-process message fabric.</summary>
public class InMemoryHost :
    BaseHost,
    IInMemoryHost
{
    readonly IInMemoryHostConfiguration _hostConfiguration;

    /// <summary>Creates an in-memory host from its transport configuration and topology.</summary>
    /// <param name="hostConfiguration">The configuration used to create endpoints and access the shared message fabric.</param>
    /// <param name="busTopology">The in-memory topology used by the host.</param>
    public InMemoryHost(IInMemoryHostConfiguration hostConfiguration, IInMemoryBusTopology busTopology)
        : base(hostConfiguration, busTopology)
    {
        _hostConfiguration = hostConfiguration;
    }

    /// <summary>Connects a receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the in-memory endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IInMemoryReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(definition, endpointNameFormatter, configure);
    }

    /// <summary>Connects an in-memory receive endpoint described by an endpoint definition.</summary>
    /// <param name="definition">The endpoint definition that supplies the name and common settings.</param>
    /// <param name="endpointNameFormatter">The formatter used to derive the queue name, or <see langword="null"/> to use the default formatter.</param>
    /// <param name="configureEndpoint">An optional callback that applies in-memory-specific endpoint settings.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IInMemoryReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        return ConnectReceiveEndpoint(queueName, configurator =>
        {
            _hostConfiguration.ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    /// <summary>Connects an in-memory receive endpoint for a named queue.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configureEndpoint">An optional provider-neutral callback applied to the in-memory endpoint configuration.</param>
    /// <returns>A handle that controls the connected receive endpoint.</returns>
    public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        Action<IInMemoryReceiveEndpointConfigurator>? configure = configureEndpoint == null
            ? null
            : endpoint => configureEndpoint(endpoint);

        return ConnectReceiveEndpoint(queueName, configure);
    }

    /// <summary>Creates, validates, builds, and starts an in-memory receive endpoint.</summary>
    /// <param name="queueName">The in-memory queue name.</param>
    /// <param name="configure">An optional callback that applies in-memory-specific endpoint settings before validation.</param>
    /// <returns>A handle that controls the started receive endpoint.</returns>
    public IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName, Action<IInMemoryReceiveEndpointConfigurator>? configure = null)
    {
        LogContext.SetCurrentIfNull(_hostConfiguration.LogContext);

        var configuration = _hostConfiguration.CreateReceiveEndpointConfiguration(queueName, configure);

        TransportLogMessages.ConnectReceiveEndpoint(configuration.InputAddress);

        configuration.Validate().ThrowIfContainsFailure("The receive endpoint configuration is invalid:");

        configuration.Build(this);

        return ReceiveEndpoints.Start(queueName);
    }

    /// <summary>Gets the delay provider used by the shared in-memory message fabric.</summary>
    public IInMemoryDelayProvider DelayProvider => _hostConfiguration.TransportProvider.MessageFabric.DelayProvider;

    /// <summary>Adds the in-memory host address and transport-provider state to a probe.</summary>
    /// <param name="context">The probe context that receives the diagnostic values.</param>
    protected override void Probe(ProbeContext context)
    {
        context.Add("type", "InMemory");
        context.Add("baseAddress", _hostConfiguration.HostAddress);

        _hostConfiguration.TransportProvider.Probe(context);
    }

    /// <summary>Returns the transport provider whose lifecycle is owned by the host.</summary>
    /// <returns>The host-owned in-memory transport provider.</returns>
    protected override IAgent[] GetAgentHandles()
    {
        return [_hostConfiguration.TransportProvider];
    }
}
