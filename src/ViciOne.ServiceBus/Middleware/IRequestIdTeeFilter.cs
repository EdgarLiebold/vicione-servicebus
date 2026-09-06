using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches consume contexts by request identifier.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IRequestIdTeeFilter<TMessage> :
    ITeeFilter<ConsumeContext<TMessage>>,
    IKeyPipeConnector<TMessage, Guid>
    where TMessage : class
{
}
