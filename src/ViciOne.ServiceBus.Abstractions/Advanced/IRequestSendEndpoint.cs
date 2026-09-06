using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Creates or sends a request through a preselected destination endpoint.</summary>
/// <typeparam name="T">The request contract type.</typeparam>
public interface IRequestSendEndpoint<T>
    where T : class
{
    /// <summary>Initializes and sends a request from property values.</summary>
    /// <param name="requestId">The identifier used to match the response.</param>
    /// <param name="values">The values used to initialize the request contract.</param>
    /// <param name="pipe">The request send-context pipeline.</param>
    /// <param name="cancellationToken">The token that cancels request creation or sending.</param>
    /// <returns>A task containing the initialized request message.</returns>
    Task<T> SendAsync(Guid requestId, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);

    /// <summary>Sends an existing request message.</summary>
    /// <param name="requestId">The identifier used to match the response.</param>
    /// <param name="message">The request message to send.</param>
    /// <param name="pipe">The request send-context pipeline.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    /// <returns>A task that completes when the destination transport has accepted the request.</returns>
    Task SendAsync(Guid requestId, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);
}
