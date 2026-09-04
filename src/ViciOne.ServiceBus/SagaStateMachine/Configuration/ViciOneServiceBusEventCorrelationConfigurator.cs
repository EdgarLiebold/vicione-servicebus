using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a state machine interface type implementation.
/// </summary>
public partial class StateMachineInterfaceType<TInstance, TData>
{
    /// <summary>
    /// Provides a vici one service bus event correlation configurator implementation.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="machine">The machine value.</param>
        /// <param name="event">The event value.</param>
        /// <param name="existingCorrelation">The existing correlation value.</param>
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

        /// <summary>
        /// Performs the build operation.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public EventCorrelation Build()
        {
            return new MessageEventCorrelation<TInstance, TData>(_machine, _event, _sagaFilterFactory, _messageFilter, _missingPipe, _sagaFactory,
                InsertOnInitial, ReadOnly, ConfigureConsumeTopology);
        }

        /// <summary>
        /// Gets or sets the insert on initial value.
        /// </summary>
        public bool InsertOnInitial { get; set; }

        /// <summary>
        /// Gets or sets the read only value.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the configure consume topology value.
        /// </summary>
        public bool ConfigureConsumeTopology { get; set; }

        /// <summary>
        /// Performs the correlate by id operation.
        /// </summary>
        /// <param name="selector">The selector value.</param>
        /// <returns>The result of the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> CorrelateById(Func<ConsumeContext<TData>, Guid> selector)
        {
            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            _sagaFilterFactory = (repository, policy, sagaPipe) => new CorrelatedSagaFilter<TInstance, TData>(repository, policy, sagaPipe);

            return this;
        }

        /// <summary>
        /// Performs the correlate by id operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="propertyExpression">The property expression value.</param>
        /// <param name="selector">The selector value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Performs the correlate by operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="propertyExpression">The property expression value.</param>
        /// <param name="selector">The selector value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Performs the correlate by operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        /// <param name="propertyExpression">The property expression value.</param>
        /// <param name="selector">The selector value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Performs the select id operation.
        /// </summary>
        /// <param name="selector">The selector value.</param>
        /// <returns>The result of the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> SelectId(Func<ConsumeContext<TData>, Guid> selector)
        {
            if (selector == null)
                throw new ArgumentNullException(nameof(selector));

            _messageFilter = new CorrelationIdMessageFilter<TData>(selector);

            return this;
        }

        /// <summary>
        /// Performs the correlate by operation.
        /// </summary>
        /// <param name="correlationExpression">The correlation expression value.</param>
        /// <returns>The result of the operation.</returns>
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

        /// <summary>
        /// Sets saga factory.
        /// </summary>
        /// <param name="factoryMethod">The factory method value.</param>
        /// <returns>The result of the operation.</returns>
        public IEventCorrelationConfigurator<TInstance, TData> SetSagaFactory(SagaFactoryMethod<TInstance, TData> factoryMethod)
        {
            _sagaFactory = new FactoryMethodSagaFactory<TInstance, TData>(factoryMethod);

            return this;
        }

        /// <summary>
        /// Performs the on missing instance operation.
        /// </summary>
        /// <param name="getMissingPipe">The get missing pipe value.</param>
        /// <returns>The result of the operation.</returns>
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
