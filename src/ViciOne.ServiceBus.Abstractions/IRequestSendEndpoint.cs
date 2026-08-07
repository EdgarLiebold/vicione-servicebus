// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public interface IRequestSendEndpoint<T>
        where T : class
    {
        Task<T> Send(Guid requestId, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);

        Task Send(Guid requestId, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);
    }
}
