using System;
using System.Collections.Generic;
using System.IO;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.RabbitMqTransport.Topology;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public class RabbitMqHostConfiguration :
    BaseHostConfiguration<IRabbitMqReceiveEndpointConfiguration, IRabbitMqReceiveEndpointConfigurator>,
    IRabbitMqHostConfiguration
{
    readonly IRabbitMqBusConfiguration _busConfiguration;
    readonly Recycle<IConnectionContextSupervisor> _connectionContext;
    readonly IRabbitMqBusTopology _topology;
    RabbitMqHostSettings _hostSettings;

    public RabbitMqHostConfiguration(IRabbitMqBusConfiguration busConfiguration, IRabbitMqTopologyConfiguration topologyConfiguration)
        : base(busConfiguration)
    {
        _busConfiguration = busConfiguration;
        _hostSettings = new ConfigurationHostSettings
        {
            Host = "localhost",
            VirtualHost = "/",
            Port = 5672,
            Username = "guest",
            Password = "guest"
        };

        var messageNameFormatter = new RabbitMqMessageNameFormatter();

        _topology = new RabbitMqBusTopology(this, messageNameFormatter, topologyConfiguration);

        ReceiveTransportRetryPolicy = Retry.CreatePolicy(x =>
        {
            // Everything the broker answers is retried, except the one answer that repeating cannot
            // change: a queue this connection cannot obtain exclusively. Retrying it kept the failed
            // endpoint start alive in the background indefinitely, so the caller never learned the
            // reason and the declare loop went on knocking at a queue that belonged to someone else.
            //
            // The exclusion sits on every rule that can carry that answer, and it took two
            // measurements to get the list right. The same refusal reaches this policy in three
            // shapes: raw as OperationInterruptedException, wrapped as RabbitMqConnectionException
            // once the transport has converted it, and — under load, when the channel is already
            // gone by the time the next operation runs — as AlreadyClosedException, which derives
            // from OperationInterruptedException and so slipped past a rule written for the base
            // type alone. Each shape left behind produced a retry loop that the isolated spec did
            // not show and the full suite did.
            x.Handle<ConnectionException>(exception => !exception.IsExclusiveResourceConflict());
            x.Handle<AlreadyClosedException>(exception => !exception.IsExclusiveResourceConflict());
            x.Handle<EndOfStreamException>();
            x.Handle<OperationInterruptedException>(exception =>
                exception.ChannelShouldBeClosed() && !exception.IsExclusiveResourceConflict());
            x.Handle<NotSupportedException>(exception => exception.Message.Contains("Pipelining of requests forbidden"));

            x.Ignore<AuthenticationFailureException>();

            x.Exponential(1000, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        });

        _connectionContext = new Recycle<IConnectionContextSupervisor>(() => new ConnectionContextSupervisor(this, topologyConfiguration));
    }

    public IConnectionContextSupervisor ConnectionContextSupervisor => _connectionContext.Supervisor;

    public override Uri HostAddress => _hostSettings.HostAddress;

    public bool PublisherConfirmation => _hostSettings.PublisherConfirmation;

    public BatchSettings BatchSettings => _hostSettings.BatchSettings;

    IRabbitMqBusTopology IRabbitMqHostConfiguration.Topology => _topology;

    public override IRetryPolicy ReceiveTransportRetryPolicy { get; }

    public override IBusTopology Topology => _topology;

    public RabbitMqHostSettings Settings
    {
        get => _hostSettings;
        set => _hostSettings = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void ApplyEndpointDefinition(IRabbitMqReceiveEndpointConfigurator configurator, IEndpointDefinition definition)
    {
        if (definition.IsTemporary)
        {
            configurator.QueueExpiration = TimeSpan.FromMinutes(1);
            configurator.AutoDelete = true;
            configurator.Durable = false;
        }

        base.ApplyEndpointDefinition(configurator, definition);
    }

    public IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IRabbitMqReceiveEndpointConfigurator>? configure)
    {
        var endpointConfiguration = _busConfiguration.CreateEndpointConfiguration();
        var settings = new RabbitMqReceiveSettings(endpointConfiguration, queueName,
            _busConfiguration.Topology.Consume.ExchangeTypeSelector.DefaultExchangeType, true, false);

        return CreateReceiveEndpointConfiguration(settings, endpointConfiguration, configure);
    }

    public IRabbitMqReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(RabbitMqReceiveSettings settings,
        IRabbitMqEndpointConfiguration endpointConfiguration, Action<IRabbitMqReceiveEndpointConfigurator>? configure)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (endpointConfiguration == null)
            throw new ArgumentNullException(nameof(endpointConfiguration));

        var configuration = new RabbitMqReceiveEndpointConfiguration(this, settings, endpointConfiguration);

        configure?.Invoke(configuration);

        Observers.EndpointConfigured(configuration);

        Add(configuration);

        return configuration;
    }

    public override void ReceiveEndpoint(IEndpointDefinition definition, IEndpointNameFormatter? endpointNameFormatter,
        Action<IRabbitMqReceiveEndpointConfigurator>? configureEndpoint = null)
    {
        var queueName = definition.GetEndpointName(endpointNameFormatter ?? DefaultEndpointNameFormatter.Instance);

        ReceiveEndpoint(queueName, configurator =>
        {
            ApplyEndpointDefinition(configurator, definition);
            configureEndpoint?.Invoke(configurator);
        });
    }

    public override void ReceiveEndpoint(string queueName, Action<IRabbitMqReceiveEndpointConfigurator> configureEndpoint)
    {
        CreateReceiveEndpointConfiguration(queueName, configureEndpoint);
    }

    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in base.Validate())
            yield return result;

        if (_hostSettings.BatchSettings.Enabled)
        {
            if (_hostSettings.BatchSettings.Timeout < TimeSpan.Zero || _hostSettings.BatchSettings.Timeout > TimeSpan.FromSeconds(1))
                yield return this.Failure("BatchTimeout", "must be >= 0 and <= 1s");

            if (_hostSettings.BatchSettings.MessageLimit <= 1 || _hostSettings.BatchSettings.MessageLimit > 100)
                yield return this.Failure("BatchMessageLimit", "must be > 1 and <= 100");

            if (_hostSettings.BatchSettings.SizeLimit < 1024 || _hostSettings.BatchSettings.MessageLimit > 256 * 1024)
                yield return this.Failure("BatchSizeLimit", "must be >= 1K and <= 256K");
        }
    }

    public override IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(string queueName,
        Action<IReceiveEndpointConfigurator>? configure = null)
    {
        return CreateReceiveEndpointConfiguration(queueName, configure);
    }

    public override IHost Build()
    {
        var host = new RabbitMqHost(this, _topology);

        foreach (var endpointConfiguration in GetConfiguredEndpoints())
            endpointConfiguration.Build(host);

        return host;
    }
}
