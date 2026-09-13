using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Marks a saga as a consumer of a correlated message that may create a saga or continue an existing one.</summary>
/// <typeparam name="TMessage">The initiating or continuing message type.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface IInitiatedByOrOrchestrates<in TMessage> :
    IConsumer<TMessage>
    where TMessage : class, ICorrelatedBy<Guid>
{
}
