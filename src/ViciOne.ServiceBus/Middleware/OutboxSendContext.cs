using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Used by the new outbox construct
/// </summary>
public interface OutboxSendContext :
    IServiceProvider
{
    /// <summary>
    /// Adds send to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
