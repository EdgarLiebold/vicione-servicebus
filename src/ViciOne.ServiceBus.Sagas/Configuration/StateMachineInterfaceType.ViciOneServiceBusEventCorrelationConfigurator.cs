using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Configures message selection, repository dispatch and missing-instance handling for a saga state-machine event.</summary>
    public class ViciOneServiceBusEventCorrelationConfigurator :
        IEventCorrelationConfigurator<TInstance, TData>,
        IEventCorrelationBuilder
    {
        readonly IEvent<TData> _event;
        readonly ISagaStateMachine<TInstance> _machine;
        IFilter<ConsumeContext<TData>>? _messageFilter;
        IPipe<ConsumeContext<TData>>? _missingPipe;
        ISagaFactory<TInstance, TData> _sagaFactory;
        SagaFilterFactory<TInstance, TData>? _sagaFilterFactory;

        /// <summary>Creates event correlation with default saga creation and consume-topology configuration.</summary>
        /// <param name="machine">The state machine handling the correlated event.</param>
        /// <param name="event">The event carrying messages to correlate with saga instances.</param>
        /// <param name="existingCorrelation">An optional matching correlation whose message filter and saga filter factory are retained.</param>
        /// <exception cref="ArgumentNullException"><paramref name="machine" /> or <paramref name="event" /> is null.</exception>
        public ViciOneServiceBusEventCorrelationConfigurator(ISagaStateMachine<TInstance> machine, IEvent<TData> @event, IEventCorrelation? existingCorrelation)
        {
            ArgumentNullException.ThrowIfNull(machine);
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));

            _event = @event;
            _machine = machine;

            InsertOnInitial = false;
            ReadOnly = false;
            ConfigureConsumeTopology = true;

            _sagaFactory = new DefaultSagaFactory<TInstance, TData>();

            if (existingCorrelation is IEventCorrelation<TInstance, TData> correlation)
            {
                _sagaFilterFactory = correlation.FilterFactory;
                _messageFilter = correlation.MessageFilter;
            }
        }

        /// <summary>Builds event correlation from the currently selected filters, factory, missing-instance pipeline and policy flags.</summary>
        /// <returns>The message-event correlation containing the current configuration.</returns>
        public IEventCorrelation Build()
        {
            return new MessageEventCorrelation<TInstance, TData>(_machine, _event, _sagaFilterFactory, _messageFilter, _missingPipe, _sagaFactory,
                InsertOnInitial, ReadOnly, ConfigureConsumeTopology);
        }

        /// <summary>Gets or sets whether an initial-state event requests saga creation before repository dispatch.</summary>
        public bool InsertOnInitial { get; set; }

        /// <summary>Gets or sets whether existing-instance dispatch uses a read-only repository policy.</summary>
        public bool ReadOnly { get; set; }

        /// <summary>Gets or sets whether connecting this event's message pipeline configures consume topology.</summary>
        public bool ConfigureConsumeTopology { get; set; }

        /// <summary>Selects a message correlation identifier and dispatches through identifier-based saga lookup.</summary>
        /// <param name="selector">The selector supplying the saga correlation identifier from each message context.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateById(Func<ConsumeContext<TData>, Guid> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) => new CorrelatedSagaFilter<TInstance, TData>(repository, policy, sagaPipe);

            return this;
        }

        /// <summary>Configures saga property-query dispatch using a selected non-default value.</summary>
        /// <typeparam name="T">The non-nullable value type used for property correlation.</typeparam>
        /// <param name="propertyExpression">The saga property queried for the selected message value.</param>
        /// <param name="selector">The selector supplying the message's property-correlation value.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyExpression" /> or <paramref name="selector" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateById<T>(Expression<Func<TInstance, T>> propertyExpression,
            Func<ConsumeContext<TData>, T> selector)
            where T : struct
        {
            ArgumentNullException.ThrowIfNull(propertyExpression);
            ArgumentNullException.ThrowIfNull(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new NotDefaultValueTypeSagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Configures saga property-query dispatch using a selected nullable value.</summary>
        /// <typeparam name="T">The underlying value type used for property correlation.</typeparam>
        /// <param name="propertyExpression">The nullable saga property queried for the selected message value.</param>
        /// <param name="selector">The selector supplying the message's nullable property-correlation value.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyExpression" /> or <paramref name="selector" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy<T>(Expression<Func<TInstance, T?>> propertyExpression,
            Func<ConsumeContext<TData>, T?> selector)
            where T : struct
        {
            ArgumentNullException.ThrowIfNull(propertyExpression);
            ArgumentNullException.ThrowIfNull(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new HasValueTypeSagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T?>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Configures saga property-query dispatch using a selected reference value.</summary>
        /// <typeparam name="T">The reference type used for property correlation.</typeparam>
        /// <param name="propertyExpression">The saga property queried for the selected message value.</param>
        /// <param name="selector">The selector supplying the message's property-correlation value.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyExpression" /> or <paramref name="selector" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy<T>(Expression<Func<TInstance, T>> propertyExpression,
            Func<ConsumeContext<TData>, T> selector)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(propertyExpression);
            ArgumentNullException.ThrowIfNull(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new SagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Selects the message correlation identifier without replacing the configured saga filter factory.</summary>
        /// <param name="selector">The selector supplying the saga correlation identifier from each message context.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="selector" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> SelectId(Func<ConsumeContext<TData>, Guid> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);

            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            return this;
        }

        /// <summary>Configures saga query dispatch using a predicate over the saga instance and message context.</summary>
        /// <param name="correlationExpression">The predicate determining which saga instances match each message.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="correlationExpression" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy(Expression<Func<TInstance, ConsumeContext<TData>, bool>> correlationExpression)
        {
            ArgumentNullException.ThrowIfNull(correlationExpression);

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var queryFactory = new ExpressionCorrelationSagaQueryFactory<TInstance, TData>(correlationExpression);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Replaces default saga creation with the supplied factory method.</summary>
        /// <param name="factoryMethod">The factory method creating saga instances from message contexts.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="factoryMethod" /> is null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> SetSagaFactory(SagaFactoryMethod<TInstance, TData> factoryMethod)
        {
            ArgumentNullException.ThrowIfNull(factoryMethod);

            _sagaFactory = new FactoryMethodSagaFactory<TInstance, TData>(factoryMethod);

            return this;
        }

        /// <summary>Configures the message pipeline used when existing-instance dispatch finds no saga.</summary>
        /// <param name="getMissingPipe">The callback building a missing-instance pipeline from the supplied configurator.</param>
        /// <returns>This configurator for further correlation configuration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="getMissingPipe" /> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="getMissingPipe" /> returns null.</exception>
        public IEventCorrelationConfigurator<TInstance, TData> OnMissingInstance(
            Func<IMissingInstanceConfigurator<TInstance, TData>, IPipe<ConsumeContext<TData>>> getMissingPipe)
        {
            ArgumentNullException.ThrowIfNull(getMissingPipe);

            var configurator = new EventMissingInstanceConfigurator<TInstance, TData>();

            _missingPipe = getMissingPipe(configurator)
                ?? throw new InvalidOperationException("The missing-instance configuration callback returned no pipe.");

            return this;
        }
    }
}
