using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.ConcurrencyLimiting;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds the routing-slip, log, activity-instance, concurrency, and observer pipelines for compensation.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class CompensateActivityHostConfigurator<TActivity, TLog> :
    ICompensateActivityConfigurator<TActivity, TLog>,
    IReceiveEndpointSpecification
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    int? _concurrentMessageLimit;
    readonly ICompensateActivityFactory<TActivity, TLog> _activityFactory;
    readonly PipeConfigurator<CompensateActivityContext<TActivity, TLog>> _activityPipeConfigurator;
    readonly PipeConfigurator<CompensateContext<TLog>> _compensatePipeConfigurator;
    readonly ActivityConfigurationObservable _configurationObservers;
    readonly ActivityObservable _observers;
    readonly RoutingSlipConfigurator _routingSlipConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Creates a compensation host configurator.</summary>
    /// <param name="activityFactory">The factory that owns compensation activity instances.</param>
    /// <param name="observer">The observer notified when the host configuration is finalized.</param>
    public CompensateActivityHostConfigurator(ICompensateActivityFactory<TActivity, TLog> activityFactory, IActivityConfigurationObserver observer)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
        ArgumentNullException.ThrowIfNull(observer);

        _activityPipeConfigurator = new PipeConfigurator<CompensateActivityContext<TActivity, TLog>>();
        _compensatePipeConfigurator = new PipeConfigurator<CompensateContext<TLog>>();
        _routingSlipConfigurator = new RoutingSlipConfigurator();
        _observers = new ActivityObservable();

        _configurationObservers = new ActivityConfigurationObservable();
        _configurationObservers.Connect(observer);
    }

    /// <summary>Adds middleware to the resolved-activity compensation pipeline.</summary>
    /// <param name="specification">The pipeline specification to add.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TActivity, TLog>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _activityPipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Gets or sets the maximum number of routing slips compensated concurrently.</summary>
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

    /// <summary>Configures middleware after the compensation log is deserialized and before the activity instance is resolved.</summary>
    /// <param name="configure">The callback that configures the log-level compensation context.</param>
    public void Log(Action<ICompensateLogConfigurator<TLog>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new CompensateLogConfigurator<TLog>(_compensatePipeConfigurator);

        configure(configurator);
    }

    /// <summary>Configures middleware after the compensation activity instance is resolved.</summary>
    /// <param name="configure">The callback that configures the activity-bound compensation context.</param>
    public void ActivityLog(Action<ICompensateActivityLogConfigurator<TLog>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var configurator = new CompensateActivityLogConfigurator<TActivity, TLog>(this);

        configure(configurator);
    }

    /// <summary>Configures middleware applied to the received routing slip before compensation begins.</summary>
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
            _configurationObservers.ForEach(observer => observer.CompensateActivityConfigured(this)));

        return _routingSlipConfigurator.Validate()
            .Concat(_activityPipeConfigurator.Validate())
            .Concat(_compensatePipeConfigurator.Validate())
            .ToArray();
    }

    /// <summary>Builds the compensation pipelines and connects them to the receive endpoint.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        _activityPipeConfigurator.UseFilter(new CompensateActivityFilter<TActivity, TLog>(_observers));

        IPipe<CompensateActivityContext<TActivity, TLog>> compensateActivityPipe = _activityPipeConfigurator.Build();

        _compensatePipeConfigurator.UseFilter(new CompensateActivityFactoryFilter<TActivity, TLog>(_activityFactory, compensateActivityPipe));

        IPipe<CompensateContext<TLog>> compensatePipe = _compensatePipeConfigurator.Build();

        if (ConcurrentMessageLimit.HasValue)
        {
            var concurrencyLimiter = new ConcurrencyLimiter(ConcurrentMessageLimit.Value, TypeCache<TActivity>.ShortName);

            _routingSlipConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<IRoutingSlip>(concurrencyLimiter));
        }

        var host = new CompensateActivityHost<TActivity, TLog>(compensatePipe);
        _routingSlipConfigurator.UseFilter(host);

        builder.ConnectConsumePipe(_routingSlipConfigurator.Build());
    }
}
