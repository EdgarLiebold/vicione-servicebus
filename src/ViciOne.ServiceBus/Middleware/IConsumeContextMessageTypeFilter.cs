using System;

namespace ViciOne.ServiceBus.Middleware;

public interface IConsumeContextMessageTypeFilter :
    IFilter<ConsumeContext>,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector
{
    ConnectHandle ConnectMessagePipe<T>(IPipe<ConsumeContext<T>> pipe)
        where T : class;

    ConnectHandle ConnectMessagePipe<T>(Guid key, IPipe<ConsumeContext<T>> pipe)
        where T : class;
}
