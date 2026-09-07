using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Specifies that a class implementing ISaga consumes TMessage as part of the saga.</summary>
/// <typeparam name="TMessage">The type of message to consume.</typeparam>
[MessageContractExclusion]
[ConsumerRegistrationExclusion]
public interface Orchestrates<in TMessage> :
    IConsumer<TMessage>
    where TMessage : class, CorrelatedBy<Guid>
{
}
