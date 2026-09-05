using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for observes.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TSaga">The t saga type.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface Observes<TMessage, TSaga> :
    IConsumer<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Gets the correlation expression value.
    /// </summary>
    Expression<Func<TSaga, TMessage, bool>> CorrelationExpression { get; }
}
