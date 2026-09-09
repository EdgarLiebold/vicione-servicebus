using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Sends requests of one contract and awaits a typed response.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
public interface IRequestClient<TRequest>
    where TRequest : class
{
    /// <summary>Sends a request and waits for a response of the specified type.</summary>
    /// <typeparam name="TResponse">The expected response message contract.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Sends a configured request and waits for a response of the specified type.</summary>
    /// <typeparam name="TResponse">The expected response message contract.</typeparam>
    /// <param name="request">The request message.</param>
    /// <param name="options">The request identity, routing metadata, headers, and delivery constraints.</param>
    /// <param name="cancellationToken">Cancels sending and response waiting.</param>
    /// <returns>A task containing the received response and its message context.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class;
}
