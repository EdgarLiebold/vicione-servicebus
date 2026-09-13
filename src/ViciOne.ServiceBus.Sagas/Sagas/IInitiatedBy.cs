using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Marks a saga as a consumer of a correlated message that must create a new saga instance.</summary>
/// <typeparam name="TMessage">The initiating message type.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface IInitiatedBy<in TMessage> :
    IConsumer<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
}
