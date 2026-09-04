using System;

namespace ViciOne.ServiceBus.Middleware;

public interface IConsumeContextOutputMessageTypeFilter<out TMessage> :
    IFilter<ConsumeContext>,
    IPipeConnector<ConsumeContext<TMessage>>,
    IConsumeMessageObserverConnector<TMessage>
    where TMessage : class
{
    ConnectHandle ConnectPipe(Guid key, IPipe<ConsumeContext<TMessage>> pipe);
}
