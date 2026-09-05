using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a receiver configuration implementation.
/// </summary>
public class ReceiverConfiguration :
    EndpointConfiguration,
    IReceiveEndpointConfigurator
{
    readonly IReceiveEndpointConfiguration _configuration;
    /// <summary>
    /// Defines the specifications value.
    /// </summary>
    protected readonly List<IReceiveEndpointSpecification> Specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    protected ReceiverConfiguration(IReceiveEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        _configuration = endpointConfiguration;

        Specifications = new List<IReceiveEndpointSpecification>();

        this.ThrowOnSkippedMessages();
        this.RethrowFaultedMessages();
    }

    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public Uri InputAddress => _configuration.InputAddress;

    /// <summary>
    /// Gets or sets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology
    {
        set => _configuration.ConfigureConsumeTopology = value;
    }

    /// <summary>
    /// Gets or sets the publish faults value.
    /// </summary>
    public bool PublishFaults
    {
        set => _configuration.PublishFaults = value;
    }

    /// <summary>
    /// Adds dependency to the configuration.
    /// </summary>
    /// <param name="dependent">The dependent value.</param>
    public void AddDependency(IReceiveEndpointDependency dependent) => _configuration.AddDependency(dependent);

    ConnectHandle IReceiveEndpointObserverConnector.ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return _configuration.ConnectReceiveEndpointObserver(observer);
    }

    /// <summary>
    /// Adds dependent to the configuration.
    /// </summary>
    /// <param name="dependent">The dependent value.</param>
    public void AddDependent(IReceiveEndpointDependent dependent) => _configuration.AddDependent(dependent);

    /// <summary>
    /// Configures message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="enabled">The enabled value.</param>
    public void ConfigureMessageTopology<T>(bool enabled = true)
        where T : class
        => _configuration.ConfigureMessageTopology<T>(enabled);

    /// <summary>
    /// Configures message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="enabled">The enabled value.</param>
    public void ConfigureMessageTopology(Type messageType, bool enabled = true) =>
        _configuration.ConfigureMessageTopology(messageType, enabled);

    /// <summary>
    /// Adds endpoint specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddEndpointSpecification(IReceiveEndpointSpecification specification)
    {
        Specifications.Add(specification ?? throw new ArgumentNullException(nameof(specification)));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        return Specifications.SelectMany(x => x.Validate())
            .Concat(base.Validate());
    }
}
