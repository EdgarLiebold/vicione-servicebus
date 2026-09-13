using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Marks a saga as a consumer of a correlated message that requires an existing saga instance.</summary>
/// <typeparam name="TMessage">The continuing message type.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface IOrchestrates<in TMessage> :
    IConsumer<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
}
