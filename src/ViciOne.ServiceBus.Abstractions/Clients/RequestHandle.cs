using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// A request handle manages the client-side request, and allows the request to be configured, response types added, etc. The handle
/// should be disposed once it is no longer in-use, and the request has been completed (successfully, or otherwise).
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface RequestHandle<TRequest> :
    RequestHandle,
    IRequestPipeConfigurator<TRequest>
    where TRequest : class
{
    /// <summary>The request message that was/will be sent.</summary>
    Task<TRequest> Message { get; }
}


/// <summary>Controls the lifetime of request.</summary>
public interface RequestHandle :
    IRequestPipeConfigurator,
    IDisposable
{
    /// <summary>If the specified result type is present, it is returned.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="readyToSend">If true, sets the request as ready to send and sends it.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>True if the result type specified is present, otherwise false.</returns>
    Task<Response<T>> GetResponseAsync<T>(bool readyToSend = true, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancel the request.</summary>
    void Cancel();
}
