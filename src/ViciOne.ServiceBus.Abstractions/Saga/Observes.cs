using System;
using System.Linq.Expressions;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by observes.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface Observes<TMessage, TSaga> :
    IConsumer<TMessage>
    where TMessage : class
{
    /// <summary>Gets the correlation expression.</summary>
    Expression<Func<TSaga, TMessage, bool>> CorrelationExpression { get; }
}
