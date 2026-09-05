namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine interface type implementation.
/// </summary>
public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>
    /// Provides a state machine saga message connector implementation.
    /// </summary>
    public class StateMachineSagaMessageConnector :
        SagaConnector<TInstance, TData>.SagaMessageConnector
    {
        readonly IFilter<ConsumeContext<TData>>? _messageFilter;
        readonly ISagaPolicy<TInstance, TData> _policy;
        readonly SagaFilterFactory<TInstance, TData>? _sagaFilterFactory;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="consumeFilter">The consume filter value.</param>
        /// <param name="policy">The policy value.</param>
        /// <param name="sagaFilterFactory">The saga filter factory value.</param>
        /// <param name="messageFilter">The message filter value.</param>
        /// <param name="configureConsumeTopology">The configure consume topology value.</param>
        public StateMachineSagaMessageConnector(IFilter<SagaConsumeContext<TInstance, TData>> consumeFilter, ISagaPolicy<TInstance, TData>? policy,
            SagaFilterFactory<TInstance, TData>? sagaFilterFactory, IFilter<ConsumeContext<TData>>? messageFilter, bool configureConsumeTopology)
            : base(consumeFilter)
        {
            ConfigureConsumeTopology = configureConsumeTopology;
            _policy = policy ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The saga event correlation did not provide a repository policy.", "Correct the named configuration before starting the host"));
            _sagaFilterFactory = sagaFilterFactory;
            _messageFilter = messageFilter;
        }

        /// <summary>
        /// Gets the configure consume topology value.
        /// </summary>
        protected override bool ConfigureConsumeTopology { get; }

        /// <summary>
        /// Configures message pipe.
        /// </summary>
        /// <param name="configurator">The configurator value.</param>
        /// <param name="repository">The repository value.</param>
        /// <param name="sagaPipe">The saga pipe value.</param>
        protected override void ConfigureMessagePipe(IPipeConfigurator<ConsumeContext<TData>> configurator, ISagaRepository<TInstance> repository,
            IPipe<SagaConsumeContext<TInstance, TData>> sagaPipe)
        {
            if (_messageFilter != null)
                configurator.UseFilter(_messageFilter);

            if (_sagaFilterFactory == null)
                throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", $"The event was not properly correlated: {TypeCache<TInstance>.ShortName} - {TypeCache<TData>.ShortName}", "Correct the named configuration before starting the host"));

            configurator.UseFilter(_sagaFilterFactory(repository, _policy, sagaPipe));
        }
    }
}
