using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides request-response operations for the in-process mediator.</summary>
public static class MediatorRequestExtensions
{
    /// <summary>Sends a request through a mediator and returns its response message.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="mediator">The mediator that handles the request.</param>
    /// <param name="request">The request message.</param>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>A task that produces the response message.</returns>
    public static async Task<T> SendRequestAsync<T>(this IMediator mediator, Request<T> request, RequestTimeout timeout = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        try
        {
            using RequestHandle<Request<T>> handle = mediator.CreateRequest(request, timeout, cancellationToken);

            Response<T> response = await handle.GetResponseAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false);

            return response.Message;
        }
        catch (RequestException exception)
        {
            if (exception.InnerException != null)
            {
                var dispatchInfo = ExceptionDispatchInfo.Capture(exception.InnerException);

                dispatchInfo.Throw();
            }

            throw;
        }
    }
}
