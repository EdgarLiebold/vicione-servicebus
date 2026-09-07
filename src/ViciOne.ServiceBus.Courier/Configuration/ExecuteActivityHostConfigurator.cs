using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures execute activity host.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityHostConfigurator<TActivity, TArguments> :
    IExecuteActivityConfigurator<TActivity, TArguments>,
    IReceiveEndpointSpecification
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    int? _concurrentMessageLimit;
    readonly IExecuteActivityFactory<TActivity, TArguments> _activityFactory;
    readonly IBuildPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _activityPipeConfigurator;
    readonly Uri? _compensateAddress;
    readonly ActivityConfigurationObservable _configurationObservers;
    readonly IBuildPipeConfigurator<ExecuteContext<TArguments>> _executePipeConfigurator;
    readonly ActivityObservable _observers;
    readonly RoutingSlipConfigurator _routingSlipConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="observer">The observer to connect.</param>
    public ExecuteActivityHostConfigurator(IExecuteActivityFactory<TActivity, TArguments> activityFactory, IActivityConfigurationObserver observer)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
        ArgumentNullException.ThrowIfNull(observer);

        _activityPipeConfigurator = new PipeConfigurator<ExecuteActivityContext<TActivity, TArguments>>();
        _executePipeConfigurator = new PipeConfigurator<ExecuteContext<TArguments>>();
        _routingSlipConfigurator = new RoutingSlipConfigurator();
        _observers = new ActivityObservable();

        _configurationObservers = new ActivityConfigurationObservable();
        _configurationObservers.Connect(observer);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    /// <param name="observer">The observer to connect.</param>
    public ExecuteActivityHostConfigurator(IExecuteActivityFactory<TActivity, TArguments> activityFactory, Uri compensateAddress,
        IActivityConfigurationObserver observer)
        : this(activityFactory, observer)
    {
        _compensateAddress = compensateAddress ?? throw new ArgumentNullException(nameof(compensateAddress));
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TActivity, TArguments>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _activityPipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Gets or sets the maximum number of routing slips executed concurrently.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrentMessageLimit;
        set
        {
            if (value is <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "The concurrent message limit must be greater than zero.");

            _concurrentMessageLimit = value;
        }
    }

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(RoutingSlip);

    /// <summary>Adds the supplied activity arguments.</summary>
    /// <param name="configure">The action that configures the deserialized activity arguments.</param>
    public void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ExecuteArgumentsConfigurator<TArguments>(_executePipeConfigurator);

        configure(configurator);
    }

    /// <summary>Adds arguments for the routing-slip activity.</summary>
    /// <param name="configure">The action that configures the activity-instance pipeline.</param>
    public void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ExecuteActivityArgumentsConfigurator<TActivity, TArguments>(_activityPipeConfigurator);

        configure(configurator);
    }

    /// <summary>Creates or configures the routing slip.</summary>
    /// <param name="configure">The action that configures the routing-slip consume pipeline.</param>
    public void RoutingSlip(Action<IRoutingSlipConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_routingSlipConfigurator);
    }

    /// <summary>Configures the activity transport-message pipeline.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configure">The action that configures the routing-slip transport-message pipeline.</param>
    public void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (typeof(TMessage) != typeof(RoutingSlip))
            throw new ArgumentException($"The activity host message type is {TypeCache<RoutingSlip>.ShortName}.", nameof(configure));

        configure((IActivityMessageConfigurator<TMessage>)(object)_routingSlipConfigurator);
    }

    /// <summary>Connects activity observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectActivityObserver(IActivityObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
        {
            _configurationObservers.ForEach(observer =>
            {
                if (_compensateAddress == null)
                    observer.ExecuteActivityConfigured(this);
                else
                    observer.ActivityConfigured(this, _compensateAddress);
            });
        });

        return _routingSlipConfigurator.Validate()
            .Concat(_activityPipeConfigurator.Validate())
            .Concat(_executePipeConfigurator.Validate())
            .ToArray();
    }

    /// <summary>Builds the execution pipelines and connects them to the receive endpoint.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _activityPipeConfigurator.UseFilter(new ExecuteActivityFilter<TActivity, TArguments>(_observers));

        IPipe<ExecuteActivityContext<TActivity, TArguments>> executeActivityPipe = _activityPipeConfigurator.Build();

        _executePipeConfigurator.UseFilter(new ExecuteActivityFactoryFilter<TActivity, TArguments>(_activityFactory, executeActivityPipe));

        IPipe<ExecuteContext<TArguments>> executePipe = _executePipeConfigurator.Build();

        if (ConcurrentMessageLimit.HasValue)
        {
            var concurrencyLimiter = new ConcurrencyLimiter(ConcurrentMessageLimit.Value, TypeCache<TActivity>.ShortName);

            _routingSlipConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<RoutingSlip>(concurrencyLimiter));
        }

        var host = new ExecuteActivityHost<TActivity, TArguments>(executePipe, _compensateAddress);
        _routingSlipConfigurator.UseFilter(host);

        builder.ConnectConsumePipe(_routingSlipConfigurator.Build());
    }
}
