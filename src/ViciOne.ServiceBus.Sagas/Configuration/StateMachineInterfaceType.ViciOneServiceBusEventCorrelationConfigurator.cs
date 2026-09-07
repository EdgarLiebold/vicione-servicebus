using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>Configures vici one service bus event correlation.</summary>
    public class ViciOneServiceBusEventCorrelationConfigurator :
        IEventCorrelationConfigurator<TInstance, TData>,
        IEventCorrelationBuilder
    {
        readonly Event<TData> _event;
        readonly SagaStateMachine<TInstance> _machine;
        IFilter<ConsumeContext<TData>>? _messageFilter = null!;
        IPipe<ConsumeContext<TData>> _missingPipe = null!;
        ISagaFactory<TInstance, TData> _sagaFactory;
        SagaFilterFactory<TInstance, TData>? _sagaFilterFactory = null!;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="machine">The machine.</param>
        /// <param name="event">The event.</param>
        /// <param name="existingCorrelation">The existing correlation.</param>
        public ViciOneServiceBusEventCorrelationConfigurator(SagaStateMachine<TInstance> machine, Event<TData> @event, EventCorrelation? existingCorrelation)
        {
            _event = @event;
            _machine = machine;

            InsertOnInitial = false;
            ReadOnly = false;
            ConfigureConsumeTopology = true;

            _sagaFactory = new DefaultSagaFactory<TInstance, TData>();

            var correlation = existingCorrelation as EventCorrelation<TInstance, TData>;
            if (correlation != null)
            {
                _sagaFilterFactory = correlation.FilterFactory;
                _messageFilter = correlation.MessageFilter;
            }
        }

        /// <summary>Builds the configured component.</summary>
        /// <returns>The configured component.</returns>
        public EventCorrelation Build()
        {
            return new MessageEventCorrelation<TInstance, TData>(_machine, _event, _sagaFilterFactory, _messageFilter, _missingPipe, _sagaFactory,
                InsertOnInitial, ReadOnly, ConfigureConsumeTopology);
        }

        /// <summary>Gets or sets the insert on initial.</summary>
        public bool InsertOnInitial { get; set; }

        /// <summary>Gets or sets the read only.</summary>
        public bool ReadOnly { get; set; }

        /// <summary>Gets or sets the configure consume topology.</summary>
        public bool ConfigureConsumeTopology { get; set; }

        /// <summary>Configures correlation using the identifier selector.</summary>
        /// <param name="selector">The selector.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateById(Func<ConsumeContext<TData>, Guid> selector)
        {
            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) => new CorrelatedSagaFilter<TInstance, TData>(repository, policy, sagaPipe);

            return this;
        }

        /// <summary>Configures correlation using the identifier selector.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateById<T>(Expression<Func<TInstance, T>> propertyExpression,
            Func<ConsumeContext<TData>, T> selector)
            where T : struct
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new NotDefaultValueTypeSagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Configures message correlation using the supplied expression.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy<T>(Expression<Func<TInstance, T?>> propertyExpression,
            Func<ConsumeContext<TData>, T?> selector)
            where T : struct
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new HasValueTypeSagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T?>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Configures message correlation using the supplied expression.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="propertyExpression">The property expression.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy<T>(Expression<Func<TInstance, T>> propertyExpression,
            Func<ConsumeContext<TData>, T> selector)
            where T : class
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var propertySelector = new SagaQueryPropertySelector<TData, T>(selector);
                var queryFactory = new PropertyExpressionSagaQueryFactory<TInstance, TData, T>(propertyExpression, propertySelector);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Selects id.</summary>
        /// <param name="selector">The selector.</param>
        /// <returns>The selected id.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> SelectId(Func<ConsumeContext<TData>, Guid> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            return this;
        }

        /// <summary>Configures message correlation using the supplied expression.</summary>
        /// <param name="correlationExpression">The correlation expression.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateBy(Expression<Func<TInstance, ConsumeContext<TData>, bool>> correlationExpression)
        {
            if (correlationExpression == null)
                throw new ArgumentNullException(nameof(correlationExpression));

            _sagaFilterFactory = (repository, policy, sagaPipe) =>
            {
                var queryFactory = new ExpressionCorrelationSagaQueryFactory<TInstance, TData>(correlationExpression);

                return new QuerySagaFilter<TInstance, TData>(repository, policy, queryFactory, sagaPipe);
            };

            return this;
        }

        /// <summary>Sets saga factory.</summary>
        /// <param name="factoryMethod">The factory method.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> SetSagaFactory(SagaFactoryMethod<TInstance, TData> factoryMethod)
        {
            _sagaFactory = new FactoryMethodSagaFactory<TInstance, TData>(factoryMethod);

            return this;
        }

        /// <summary>Handles the notification for missing instance.</summary>
        /// <param name="getMissingPipe">The get missing pipe.</param>
        /// <returns>The event correlation configurator produced by the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> OnMissingInstance(
            Func<IMissingInstanceConfigurator<TInstance, TData>, IPipe<ConsumeContext<TData>>> getMissingPipe)
        {
            if (getMissingPipe == null)
                throw new ArgumentNullException(nameof(getMissingPipe));

            var configurator = new EventMissingInstanceConfigurator<TInstance, TData>();

            _missingPipe = getMissingPipe(configurator);

            return this;
        }
    }
}
