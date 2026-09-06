using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures compensate activity host.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateActivityHostConfigurator<TActivity, TLog> :
    ICompensateActivityConfigurator<TActivity, TLog>,
    IReceiveEndpointSpecification
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    readonly ICompensateActivityFactory<TActivity, TLog> _activityFactory;
    readonly IBuildPipeConfigurator<CompensateActivityContext<TActivity, TLog>> _activityPipeConfigurator;
    readonly IBuildPipeConfigurator<CompensateContext<TLog>> _compensatePipeConfigurator;
    readonly ActivityConfigurationObservable _configurationObservers;
    readonly ActivityObservable _observers;
    readonly RoutingSlipConfigurator _routingSlipConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <param name="observer">The observer to connect.</param>
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

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<CompensateActivityContext<TActivity, TLog>> specification)
    {
        _activityPipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(RoutingSlip);

    /// <summary>Writes the current diagnostic event.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Log(Action<ICompensateLogConfigurator<TLog>> configure)
    {
        var configurator = new CompensateLogConfigurator<TLog>(_compensatePipeConfigurator);

        configure?.Invoke(configurator);
    }

    /// <summary>Adds an activity log to the routing slip.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ActivityLog(Action<ICompensateActivityLogConfigurator<TLog>> configure)
    {
        var configurator = new CompensateActivityLogConfigurator<TActivity, TLog>(this);

        configure?.Invoke(configurator);
    }

    /// <summary>Creates or configures the routing slip.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    public void RoutingSlip(Action<IRoutingSlipConfigurator> configure)
    {
        configure?.Invoke(_routingSlipConfigurator);
    }

    /// <summary>Configures the activity transport-message pipeline.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
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

    /// <summary>Applies the supplied configuration.</summary>
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

            _routingSlipConfigurator.AddPipeSpecification(new ConcurrencyLimitConsumePipeSpecification<RoutingSlip>(concurrencyLimiter));
        }

        var host = new CompensateActivityHost<TActivity, TLog>(compensatePipe);
        _routingSlipConfigurator.UseFilter(host);

        builder.ConnectConsumePipe(_routingSlipConfigurator.Build());
    }
}
