// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System;


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
}
