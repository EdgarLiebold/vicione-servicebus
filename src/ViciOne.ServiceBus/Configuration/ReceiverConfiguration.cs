using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Stores and validates receiver configuration.</summary>
public class ReceiverConfiguration :
    EndpointConfiguration,
    IReceiveEndpointConfigurator
{
    readonly IReceiveEndpointConfiguration _configuration;
    /// <summary>Exposes the specifications used by the containing type.</summary>
    protected readonly List<IReceiveEndpointSpecification> Specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="endpointConfiguration">The endpoint configuration.</param>
    protected ReceiverConfiguration(IReceiveEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _configuration = endpointConfiguration;

        Specifications = new List<IReceiveEndpointSpecification>();

        this.ThrowOnSkippedMessages();
        this.RethrowFaultedMessages();
    }

    /// <summary>Gets the input address.</summary>
    public Uri InputAddress => _configuration.InputAddress;

    /// <summary>Gets or sets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology
    {
        set => _configuration.ConfigureConsumeTopology = value;
    }

    /// <summary>Gets or sets the publish faults.</summary>
    public bool PublishFaults
    {
        set => _configuration.PublishFaults = value;
    }

    /// <summary>Adds dependency to the configuration.</summary>
    /// <param name="dependent">The dependent.</param>
    public void AddDependency(IReceiveEndpointDependency dependent) => _configuration.AddDependency(dependent);

    ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _configuration.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>Adds dependent to the configuration.</summary>
    /// <param name="dependent">The dependent.</param>
    public void AddDependent(IReceiveEndpointDependent dependent) => _configuration.AddDependent(dependent);

    /// <summary>Configures message topology.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="enabled">The enabled.</param>
    public void ConfigureMessageTopology<T>(bool enabled = true)
        where T : class
        => _configuration.ConfigureMessageTopology<T>(enabled);

    /// <summary>Configures message topology.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="enabled">The enabled.</param>
    public void ConfigureMessageTopology(Type messageType, bool enabled = true) =>
        _configuration.ConfigureMessageTopology(messageType, enabled);

    /// <summary>Adds endpoint specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddEndpointSpecification(IReceiveEndpointSpecification specification)
    {
        Specifications.Add(specification ?? throw new ArgumentNullException(nameof(specification)));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return Specifications.SelectMany(x => x.Validate())
            .Concat(base.Validate());
    }
}
