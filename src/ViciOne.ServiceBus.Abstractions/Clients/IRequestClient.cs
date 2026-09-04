using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Provides the application-level request/response operation for a request message type.</summary>
public interface IRequestClient<TRequest>
    where TRequest : class
{
    /// <summary>Sends a request and waits for a response of the specified type.</summary>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class;

    /// <summary>Sends a configured request and waits for a response of the specified type.</summary>
    /// <param name="request">The request used by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<Response<TResponse>> GetResponseAsync<TResponse>(TRequest request, RequestOptions options,
        CancellationToken cancellationToken = default)
        where TResponse : class;
}
