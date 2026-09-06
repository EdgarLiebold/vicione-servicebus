using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Used to create a saga query from the message consume context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISagaQueryFactory<TSaga, in TMessage> :
    IProbeSite
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Creates a saga query from the specified message context.</summary>
    /// <param name="context">The message context.</param>
    /// <param name="query">Receives the query produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryCreateQuery(ConsumeContext<TMessage> context, [NotNullWhen(true)] out ISagaQuery<TSaga>? query);
}
