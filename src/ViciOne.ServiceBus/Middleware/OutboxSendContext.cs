using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Used by the new outbox construct
/// </summary>
public interface OutboxSendContext :
    IServiceProvider
{
    Task AddSend<T>(SendContext<T> context)
        where T : class;
}
