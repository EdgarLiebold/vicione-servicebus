using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Defines the operations required by request send endpoint.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IRequestSendEndpoint<T>
    where T : class
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="values">The values.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the send outcome.</returns>
    Task<T> SendAsync(Guid requestId, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="requestId">The request id.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(Guid requestId, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);
}
