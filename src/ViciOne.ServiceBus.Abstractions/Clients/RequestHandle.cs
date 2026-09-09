using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Configures one request and owns its response registrations until disposal.</summary>
/// <typeparam name="TRequest">The request message contract.</typeparam>
public interface RequestHandle<TRequest> :
    RequestHandle,
    IRequestPipeConfigurator<TRequest>
    where TRequest : class
{
    /// <summary>Gets the request message accepted by the send path.</summary>
    Task<TRequest> Message { get; }
}


/// <summary>Controls the response registrations and lifetime of one pending request.</summary>
public interface RequestHandle :
    IRequestPipeConfigurator,
    IDisposable
{
    /// <summary>Registers a response contract and waits for its matching response.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="readyToSend">Whether this is the final response registration and the request may be sent.</param>
    /// <param name="cancellationToken">Cancels waiting for this response.</param>
    /// <returns>A task containing the matching response.</returns>
    Task<Response<T>> GetResponseAsync<T>(bool readyToSend = true, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Cancels sending and response waiting for this request.</summary>
    void Cancel();
}
