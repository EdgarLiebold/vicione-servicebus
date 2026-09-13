using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the routing-slip, argument, activity-instance, concurrency, and observer pipelines for execution.</summary>
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
    readonly PipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _activityPipeConfigurator;
    readonly Uri? _compensateAddress;
    readonly ActivityConfigurationObservable _configurationObservers;
    readonly PipeConfigurator<ExecuteContext<TArguments>> _executePipeConfigurator;
    readonly ActivityObservable _observers;
    readonly RoutingSlipConfigurator _routingSlipConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates an execution-only host configurator.</summary>
    /// <param name="activityFactory">The factory that owns execution activity instances.</param>
    /// <param name="observer">The observer notified when the host configuration is finalized.</param>
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

    /// <summary>Creates a compensatable execution host configurator.</summary>
    /// <param name="activityFactory">The factory that owns execution activity instances.</param>
    /// <param name="compensateAddress">The endpoint that compensates successful executions.</param>
    /// <param name="observer">The observer notified when the host configuration is finalized.</param>
    public ExecuteActivityHostConfigurator(IExecuteActivityFactory<TActivity, TArguments> activityFactory, Uri compensateAddress,
        IActivityConfigurationObserver observer)
        : this(activityFactory, observer)
    {
        _compensateAddress = compensateAddress ?? throw new ArgumentNullException(nameof(compensateAddress));
    }

    /// <summary>Adds middleware to the resolved-activity execution pipeline.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
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

    /// <summary>Gets the routing-slip transport contract consumed by this host.</summary>
    public Type MessageType => typeof(IRoutingSlip);

    /// <summary>Configures middleware after arguments are deserialized and before the activity instance is resolved.</summary>
    /// <param name="configure">The callback that configures the argument-level execution context.</param>
    public void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ExecuteArgumentsConfigurator<TArguments>(_executePipeConfigurator);

        configure(configurator);
    }

    /// <summary>Configures middleware after the activity instance is resolved.</summary>
    /// <param name="configure">The callback that configures the activity-bound execution context.</param>
    public void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new ExecuteActivityArgumentsConfigurator<TActivity, TArguments>(_activityPipeConfigurator);

        configure(configurator);
    }

    /// <summary>Configures middleware applied to the received routing slip before execution begins.</summary>
    /// <param name="configure">The action that configures the routing-slip consume pipeline.</param>
    public void RoutingSlip(Action<IRoutingSlipConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(_routingSlipConfigurator);
    }

    /// <summary>Configures the transport-message pipeline when its contract is the routing slip consumed by this host.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configure">The action that configures the routing-slip transport-message pipeline.</param>
    public void Message<TMessage>(Action<IActivityMessageConfigurator<TMessage>> configure)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (typeof(TMessage) != typeof(IRoutingSlip))
            throw new InvalidOperationException($"The activity host message type is {TypeCache<IRoutingSlip>.ShortName}, not {TypeCache<TMessage>.ShortName}.");

        configure((IActivityMessageConfigurator<TMessage>)(object)_routingSlipConfigurator);
    }

    /// <summary>Connects an observer to execution and compensation activity lifecycles.</summary>
    /// <param name="observer">The activity observer to connect.</param>
    /// <returns>A handle that disconnects the observer.</returns>
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

            _routingSlipConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<IRoutingSlip>(concurrencyLimiter));
        }

        var host = new ExecuteActivityHost<TActivity, TArguments>(executePipe, _compensateAddress);
        _routingSlipConfigurator.UseFilter(host);

        builder.ConnectConsumePipe(_routingSlipConfigurator.Build());
    }
}
