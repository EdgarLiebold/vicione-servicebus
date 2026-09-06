using System;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Dispatches consume contexts by request identifier.</summary>
public interface IRequestIdTeeFilter<TMessage> :
    ITeeFilter<ConsumeContext<TMessage>>,
    IKeyPipeConnector<TMessage, Guid>
    where TMessage : class
{
}
