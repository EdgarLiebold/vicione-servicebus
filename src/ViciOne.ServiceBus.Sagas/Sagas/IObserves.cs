using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Marks a saga as a consumer that selects existing saga instances with a custom expression.</summary>
/// <typeparam name="TMessage">The observed message type.</typeparam>
/// <typeparam name="TSaga">The saga state type queried by the expression.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface IObserves<TMessage, TSaga> :
    IConsumer<TMessage>
    where TMessage : class
{
    /// <summary>Gets the expression that matches existing saga instances to an observed message.</summary>
    Expression<Func<TSaga, TMessage, bool>> CorrelationExpression { get; }
}
