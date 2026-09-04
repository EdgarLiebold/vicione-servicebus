using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a receive endpoint configuration implementation.
/// </summary>
public abstract class ReceiveEndpointConfiguration :
    EndpointConfiguration,
    IReceiveEndpointConfiguration
{
    readonly Lazy<IConsumePipe> _consumePipe;
    readonly HashSet<IReceiveEndpointDependency> _dependencies;
    readonly HashSet<IReceiveEndpointDependent> _dependents;
    readonly List<string> _lateConfigurationKeys;
    readonly List<IReceiveEndpointSpecification> _specifications;
    IReceiveEndpoint _receiveEndpoint = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    protected ReceiveEndpointConfiguration(IHostConfiguration hostConfiguration, IEndpointConfiguration endpointConfiguration)
        : base(endpointConfiguration)
    {
        ConfigureConsumeTopology = true;
        PublishFaults = true;

        _consumePipe = new Lazy<IConsumePipe>(() => Consume.Specification.BuildConsumePipe());
        _specifications = new List<IReceiveEndpointSpecification>();
        _lateConfigurationKeys = new List<string>();
        _dependencies = new HashSet<IReceiveEndpointDependency>();
        _dependents = new HashSet<IReceiveEndpointDependent>();

        EndpointObservers = new ReceiveEndpointObservable();
        ReceiveObservers = new ReceiveObservable();
        TransportObservers = new ReceiveTransportObservable();

        ConnectConsumerConfigurationObserver(hostConfiguration.BusConfiguration);
        ConnectSagaConfigurationObserver(hostConfiguration.BusConfiguration);
        ConnectHandlerConfigurationObserver(hostConfiguration.BusConfiguration);
        ConnectActivityConfigurationObserver(hostConfiguration.BusConfiguration);
    }

    /// <summary>
    /// Gets the endpoint observers value.
    /// </summary>
    public ReceiveEndpointObservable EndpointObservers { get; }
    /// <summary>
    /// Gets the receive observers value.
    /// </summary>
    public ReceiveObservable ReceiveObservers { get; }
    /// <summary>
    /// Gets the transport observers value.
    /// </summary>
    public ReceiveTransportObservable TransportObservers { get; }

    /// <summary>
    /// Gets or sets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; set; }
    /// <summary>
    /// Gets or sets the publish faults value.
    /// </summary>
    public bool PublishFaults { get; set; }

    /// <summary>
    /// Connects receive endpoint observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer)
    {
        return EndpointObservers.Connect(observer);
    }

    /// <summary>
    /// Adds dependent to the configuration.
    /// </summary>
    /// <param name="dependent">The dependent value.</param>
    public void AddDependent(IReceiveEndpointDependent dependent)
    {
        _dependents.Add(dependent);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in _specifications.SelectMany(x => x.Validate()))
            yield return result;

        foreach (var result in _lateConfigurationKeys.Select(x => this.Failure(x, "was modified after being used")))
            yield return result;

        foreach (var result in base.Validate())
            yield return result;
    }

    /// <summary>
    /// Gets the consume pipe value.
    /// </summary>
    public IConsumePipe ConsumePipe => _consumePipe.Value;

    /// <summary>
    /// Gets the host address value.
    /// </summary>
    public abstract Uri HostAddress { get; }
    /// <summary>
    /// Gets the input address value.
    /// </summary>
    public abstract Uri InputAddress { get; }

    /// <summary>
    /// Gets or sets the receive endpoint value.
    /// </summary>
    public virtual IReceiveEndpoint ReceiveEndpoint
    {
        get
        {
            if (_receiveEndpoint == null)
                throw new InvalidOperationException("The receive endpoint has not been built.");

            return _receiveEndpoint;
        }

        protected set => _receiveEndpoint = value;
    }

    /// <summary>
    /// Creates receive pipe.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public virtual IReceivePipe CreateReceivePipe()
    {
        return Receive.CreatePipe(CreateConsumePipe(), Serialization.CreateSerializerCollection());
    }

    /// <summary>
    /// Creates receive endpoint context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public abstract ReceiveEndpointContext CreateReceiveEndpointContext();

    /// <summary>
    /// Gets the dependencies ready value.
    /// </summary>
    public Task DependenciesReady => Task.WhenAll(_dependencies.Select(x => x.Ready));

    /// <summary>
    /// Gets the dependents completed value.
    /// </summary>
    public Task DependentsCompleted => Task.WhenAll(_dependents.Select(x => x.Completed));

    /// <summary>
    /// Configures message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="enabled">The enabled value.</param>
    public void ConfigureMessageTopology<T>(bool enabled = true)
        where T : class
    {
        Topology.Consume.GetMessageTopology<T>().ConfigureConsumeTopology = enabled;
    }

    /// <summary>
    /// Configures message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="enabled">The enabled value.</param>
    public void ConfigureMessageTopology(Type messageType, bool enabled = true)
    {
        Topology.Consume.GetMessageTopology(messageType).ConfigureConsumeTopology = enabled;
    }

    /// <summary>
    /// Adds dependency to the configuration.
    /// </summary>
    /// <param name="dependency">The dependency value.</param>
    public void AddDependency(IReceiveEndpointDependency dependency)
    {
        _dependencies.Add(dependency);
    }

    /// <summary>
    /// Performs the apply specifications operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    protected void ApplySpecifications(IReceiveEndpointBuilder builder)
    {
        for (var i = 0; i < _specifications.Count; i++)
            _specifications[i].Configure(builder);
    }

    /// <summary>
    /// Adds endpoint specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddEndpointSpecification(IReceiveEndpointSpecification specification)
    {
        _specifications.Add(specification);
    }

    /// <summary>
    /// Creates consume pipe.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected virtual IConsumePipe CreateConsumePipe()
    {
        return _consumePipe.Value;
    }

    /// <summary>
    /// Performs the changed operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    protected void Changed(string key)
    {
        if (IsAlreadyConfigured())
            _lateConfigurationKeys.Add(key);
    }

    /// <summary>
    /// Determines whether already configured.
    /// </summary>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected virtual bool IsAlreadyConfigured()
    {
        return false;
    }
}
