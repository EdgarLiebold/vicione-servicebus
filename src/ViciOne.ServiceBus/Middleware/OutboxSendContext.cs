// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Middleware
{
    using System;
    using System.Threading.Tasks;


    /// <summary>
    /// Used by the new outbox construct
    /// </summary>
    public interface OutboxSendContext :
        IServiceProvider
    {
        Task AddSend<T>(SendContext<T> context)
            where T : class;
    }
}
