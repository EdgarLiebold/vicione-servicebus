namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Connects a state-machine event's message pipeline to correlated saga repository dispatch.</summary>
    public class StateMachineSagaMessageConnector :
        SagaConnector<TInstance, TData>.SagaMessageConnector
    {
        readonly IFilter<ConsumeContext<TData>>? _messageFilter;
        readonly ISagaPolicy<TInstance, TData> _policy;
        readonly SagaFilterFactory<TInstance, TData>? _sagaFilterFactory;

        /// <summary>Associates state-machine consumption with the required repository policy and optional dispatch filters.</summary>
        /// <param name="consumeFilter">The filter executing the state-machine event in a saga context.</param>
        /// <param name="policy">The existing/missing-instance policy; a missing policy is rejected.</param>
        /// <param name="sagaFilterFactory">The correlation factory required when the message pipeline is connected.</param>
        /// <param name="messageFilter">The optional filter applied before saga repository dispatch.</param>
        /// <param name="configureConsumeTopology">Whether connecting the message pipeline configures consume topology.</param>
        /// <exception cref="ArgumentNullException"><paramref name="consumeFilter" /> is null.</exception>
        /// <exception cref="ConfigurationException"><paramref name="policy" /> is null.</exception>
        public StateMachineSagaMessageConnector(IFilter<SagaConsumeContext<TInstance, TData>> consumeFilter, ISagaPolicy<TInstance, TData>? policy,
            SagaFilterFactory<TInstance, TData>? sagaFilterFactory, IFilter<ConsumeContext<TData>>? messageFilter, bool configureConsumeTopology)
            : base(consumeFilter)
        {
            ConfigureConsumeTopology = configureConsumeTopology;
            _policy = policy ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The saga event correlation did not provide a repository policy.", "Correct the named configuration before starting the host"));
            _sagaFilterFactory = sagaFilterFactory;
            _messageFilter = messageFilter;
        }

        /// <summary>Gets the correlation's selection for consume-topology configuration.</summary>
        protected override bool ConfigureConsumeTopology { get; }

        /// <summary>Appends an optional message filter and the required correlated saga-dispatch filter.</summary>
        /// <param name="configurator">The message pipeline receiving the dispatch filters.</param>
        /// <param name="repository">The repository locating or creating saga instances under the configured policy.</param>
        /// <param name="sagaPipe">The pipeline invoking the state machine with the selected saga context.</param>
        /// <exception cref="ArgumentNullException">A required method argument is null.</exception>
        /// <exception cref="ConfigurationException">The event correlation did not supply a saga filter factory.</exception>
        /// <exception cref="InvalidOperationException">The saga filter factory returned a null filter.</exception>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TData>> configurator, ISagaRepository<TInstance> repository,
            IPipe<SagaConsumeContext<TInstance, TData>> sagaPipe)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(sagaPipe);

            if (_sagaFilterFactory == null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", $"The event was not properly correlated: {TypeCache<TInstance>.ShortName} - {TypeCache<TData>.ShortName}", "Correct the named configuration before starting the host"));

            IFilter<ConsumeContext<TData>> sagaFilter = _sagaFilterFactory(repository, _policy, sagaPipe)
                ?? throw new InvalidOperationException(
                    $"The saga filter factory returned a null filter: {TypeCache<TInstance>.ShortName} - {TypeCache<TData>.ShortName}.");

            if (_messageFilter != null)
                configurator.UseFilter(_messageFilter);

            configurator.UseFilter(sagaFilter);
        }
    }
}
