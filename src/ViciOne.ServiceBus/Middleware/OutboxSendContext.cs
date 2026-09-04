using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Used by the new outbox construct
/// </summary>
public interface OutboxSendContext :
    IServiceProvider
{
    Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
