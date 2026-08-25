namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Courier;
    using Courier.Contracts;
    using Middleware;
    using Observables;


    public class ExecuteActivityHostConfigurator<TActivity, TArguments> :
        IExecuteActivityConfigurator<TActivity, TArguments>,
        IReceiveEndpointSpecification
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        readonly IExecuteActivityFactory<TActivity, TArguments> _activityFactory;
        readonly IBuildPipeConfigurator<ExecuteActivityContext<TActivity, TArguments>> _activityPipeConfigurator;
        readonly Uri _compensateAddress;
        readonly ActivityConfigurationObservable _configurationObservers;
        readonly IBuildPipeConfigurator<ExecuteContext<TArguments>> _executePipeConfigurator;
        readonly ActivityObservable _observers;
        readonly RoutingSlipConfigurator _routingSlipConfigurator;
        readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();

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

        public ExecuteActivityHostConfigurator(IExecuteActivityFactory<TActivity, TArguments> activityFactory, Uri compensateAddress,
            IActivityConfigurationObserver observer)
            : this(activityFactory, observer)
        {
            _compensateAddress = compensateAddress ?? throw new ArgumentNullException(nameof(compensateAddress));
        }

        public void AddPipeSpecification(IPipeSpecification<ExecuteActivityContext<TActivity, TArguments>> specification)
        {
            _activityPipeConfigurator.AddPipeSpecification(specification);
        }

        public int? ConcurrentMessageLimit { get; set; }

        public void Arguments(Action<IExecuteArgumentsConfigurator<TArguments>> configure)
        {
            var configurator = new ExecuteArgumentsConfigurator<TArguments>(_executePipeConfigurator);

            configure?.Invoke(configurator);
        }

        public void ActivityArguments(Action<IExecuteActivityArgumentsConfigurator<TArguments>> configure)
        {
            var configurator = new ExecuteActivityArgumentsConfigurator<TActivity, TArguments>(_activityPipeConfigurator);

            configure?.Invoke(configurator);
        }

        public void RoutingSlip(Action<IRoutingSlipConfigurator> configure)
        {
            configure?.Invoke(_routingSlipConfigurator);
        }

        public ConnectHandle ConnectActivityObserver(IActivityObserver observer)
        {
            return _observers.Connect(observer);
        }

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
}
