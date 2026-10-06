using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Configures event correlation.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IEventCorrelationConfigurator<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
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
    /// <returns>This configurator for further correlation configuration.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> SelectId(Func<ConsumeContext<TMessage>, Guid> selector);

    /// <summary>Specify the correlation expression for the event.</summary>
    /// <param name="correlationExpression">The correlation expression.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> CorrelateBy(Expression<Func<TSaga, ConsumeContext<TMessage>, bool>> correlationExpression);

    /// <summary>
    /// Configures the factory used to create new saga instances. When pre-insertion is enabled,
    /// duplicate detection and insertion failure handling follow the repository provider's policy.
    /// </summary>
    /// <param name="factoryMethod">The factory method for the saga.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    IEventCorrelationConfigurator<TSaga, TMessage> SetSagaFactory(SagaFactoryMethod<TSaga, TMessage> factoryMethod);

    /// <summary>
    /// Configures the message pipeline used when a non-initial event has no matching saga instance.
    /// </summary>
    /// <remarks>
    /// Without a configured missing-instance pipeline, the event completes through an empty pipeline.
    /// Initial-state events use the configured saga factory instead. The callback may select discard, fault, or custom behavior.
    /// An explicitly selected fault or an exception from custom behavior propagates to the surrounding receive pipeline;
    /// its configured error policy determines subsequent fault reporting and error-transport handling.
    /// </remarks>
    /// <param name="getBehavior">The callback building the missing-instance pipeline from the supplied configurator.</param>
    /// <returns>The event correlation configurator produced by the operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="getBehavior" /> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="getBehavior" /> returns null.</exception>
    IEventCorrelationConfigurator<TSaga, TMessage> OnMissingInstance(Func<IMissingInstanceConfigurator<TSaga, TMessage>,
        IPipe<ConsumeContext<TMessage>>> getBehavior);
}
