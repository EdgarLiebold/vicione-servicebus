// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Specifies that a class implementing ISaga consumes TMessage as part of the saga
    /// </summary>
    /// <typeparam name="TMessage">The type of message to consume</typeparam>
    public interface Orchestrates<in TMessage> :
        IConsumer<TMessage>
        where TMessage : class, CorrelatedBy<Guid>
    {
    }
}
