namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Connects state machine saga message to the service bus pipeline.</summary>
    public class StateMachineSagaMessageConnector :
        SagaConnector<TInstance, TData>.SagaMessageConnector
    {
        readonly IFilter<ConsumeContext<TData>>? _messageFilter;
        readonly ISagaPolicy<TInstance, TData> _policy;
        readonly SagaFilterFactory<TInstance, TData>? _sagaFilterFactory;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="consumeFilter">The consume filter.</param>
        /// <param name="policy">The policy.</param>
        /// <param name="sagaFilterFactory">The saga filter factory.</param>
        /// <param name="messageFilter">The message filter.</param>
        /// <param name="configureConsumeTopology">The configure consume topology.</param>
        public StateMachineSagaMessageConnector(IFilter<SagaConsumeContext<TInstance, TData>> consumeFilter, ISagaPolicy<TInstance, TData>? policy,
            SagaFilterFactory<TInstance, TData>? sagaFilterFactory, IFilter<ConsumeContext<TData>>? messageFilter, bool configureConsumeTopology)
            : base(consumeFilter)
        {
            ConfigureConsumeTopology = configureConsumeTopology;
            _policy = policy ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Saga", "unknown", "The saga event correlation did not provide a repository policy.", "Correct the named configuration before starting the host"));
            _sagaFilterFactory = sagaFilterFactory;
            _messageFilter = messageFilter;
        }

        /// <summary>Gets the configure consume topology.</summary>
        protected override bool ConfigureConsumeTopology { get; }

        /// <summary>Configures message pipe.</summary>
        /// <param name="configurator">The configurator to update.</param>
        /// <param name="repository">The repository.</param>
        /// <param name="sagaPipe">The saga pipe.</param>
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
