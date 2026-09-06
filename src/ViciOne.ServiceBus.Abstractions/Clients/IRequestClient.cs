using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Provides the application-level request/response operation for a request message type.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IRequestClient<TRequest>
    where TRequest : class
{
    /// <summary>Sends a request and waits for a response of the specified type.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Sends a configured request and waits for a response of the specified type.</summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class;
}
