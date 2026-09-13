using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures event correlation.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IEventCorrelationConfigurator<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// When enabled, the repository may insert a new saga instance before acquiring the normal read lock,
    /// using weaker isolation to avoid database range locks that serialize otherwise independent inserts.
    /// </summary>
    bool InsertOnInitial { set; }

    /// <summary>
    /// When enabled, the repository does not persist changes made while handling the event. The in-memory repository
    /// cannot provide this isolation because handlers operate on its stored instance directly.
    /// </summary>
    bool ReadOnly { set; }

    /// <summary>If set to false, the event type will not be configured as part of the broker topology.</summary>
    bool ConfigureConsumeTopology { set; }

    /// <summary>Correlate to the saga instance by CorrelationId, using the id from the event data.</summary>
    /// <param name="selector">Returns the CorrelationId from the event data.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateById(Func<ConsumeContext<TMessage>, Guid> selector);

    /// <summary>Correlate to the saga instance by a single value property, matched to the property value of the message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="propertyExpression">The instance property.</param>
    /// <param name="selector">The identifier selector for the message.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateById<T>(Expression<Func<TSaga, T>> propertyExpression,
        Func<ConsumeContext<TMessage>, T> selector)
        where T : struct;

    /// <summary>Correlate to the saga instance by a single property, matched to the property value of the message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="propertyExpression">The instance property.</param>
    /// <param name="selector">The selector.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateBy<T>(Expression<Func<TSaga, T?>> propertyExpression,
        Func<ConsumeContext<TMessage>, T?> selector)
        where T : struct;

    /// <summary>Correlate to the saga instance by a single property, matched to the property value of the message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="propertyExpression">The instance property.</param>
    /// <param name="selector">The selector.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateBy<T>(Expression<Func<TSaga, T>> propertyExpression, Func<ConsumeContext<TMessage>, T> selector)
        where T : class;

    /// <summary>When creating a new saga instance, initialize the saga CorrelationId with the id from the event data.</summary>
    /// <param name="selector">Returns the CorrelationId from the event data.</param>
    /// <returns>The selected id.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> SelectId(Func<ConsumeContext<TMessage>, Guid> selector);

    /// <summary>Specify the correlation expression for the event.</summary>
    /// <param name="correlationExpression">The correlation expression.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateBy(Expression<Func<TSaga, ConsumeContext<TMessage>, bool>> correlationExpression);

    /// <summary>
    /// Creates a new instance of the saga, and if appropriate, pre-inserts the saga instance to the database. If the saga already exists, any
    /// exceptions from the insert are suppressed and processing continues normally.
    /// </summary>
    /// <param name="factoryMethod">The factory method for the saga.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> SetSagaFactory(SagaFactoryMethod<TSaga, TMessage> factoryMethod);

    /// <summary>
    /// If an event is consumed that is not matched to an existing saga instance, discard the event without throwing an exception.
    /// The default behavior is to throw an exception, which moves the event into the error queue for later processing.
    /// </summary>
    /// <param name="getBehavior">The configuration call to specify the behavior on missing instance.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> OnMissingInstance(Func<IMissingInstanceConfigurator<TSaga, TMessage>,
        IPipe<ConsumeContext<TMessage>>> getBehavior);
}
