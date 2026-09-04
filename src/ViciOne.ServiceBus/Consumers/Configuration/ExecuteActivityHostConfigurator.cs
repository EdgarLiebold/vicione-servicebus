using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an execute activity host configurator implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityHostConfigurator<TActivity, TArguments> :
    IExecuteActivityConfigurator<TActivity, TArguments>,
    IReceiveEndpointSpecification
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    readonly IExecuteActivityFactory<TActivity, TArguments> _activityFactory;
    readonly IBuildPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _activityPipeConfigurator;
    readonly Uri _compensateAddress = null!;
    readonly ActivityConfigurationObservable _configurationObservers;
    readonly IBuildPipeConfigurator<ExecuteContext<TArguments>> _executePipeConfigurator;
    readonly ActivityObservable _observers;
    readonly RoutingSlipConfigurator _routingSlipConfigurator;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityFactory">The activity factory value.</param>
    /// <param name="observer">The observer value.</param>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityFactory">The activity factory value.</param>
    /// <param name="compensateAddress">The compensate address value.</param>
    /// <param name="observer">The observer value.</param>
    public ExecuteActivityHostConfigurator(IExecuteActivityFactory<TActivity, TArguments> activityFactory, Uri compensateAddress,
        IActivityConfigurationObserver observer)
        : this(activityFactory, observer)
    {
        _compensateAddress = compensateAddress ?? throw new ArgumentNullException(nameof(compensateAddress));
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TActivity, TArguments>> specification)
    {
        _activityPipeConfigurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit { get; set; }

    /// <summary>
    /// Performs the arguments operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure)
    {
        var configurator = new ExecuteArgumentsConfigurator<TArguments>(_executePipeConfigurator);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the activity arguments operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure)
    {
        var configurator = new ExecuteActivityArgumentsConfigurator<TActivity, TArguments>(_activityPipeConfigurator);

        configure?.Invoke(configurator);
    }

    /// <summary>
    /// Performs the routing slip operation.
    /// </summary>
    /// <param name="configure">The configuration callback.</param>
    public void RoutingSlip(Action<IRoutingSlipConfigurator> configure)
    {
        configure?.Invoke(_routingSlipConfigurator);
    }

    /// <summary>
    /// Connects activity observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectActivityObserver(IActivityObserver observer)
    {
        return _observers.Connect(observer);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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
