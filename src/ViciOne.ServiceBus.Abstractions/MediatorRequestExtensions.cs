using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Mediator;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides extension methods for mediator request.
/// </summary>
public static class MediatorRequestExtensions
{
    /// <summary>
    /// Sends a request, with the specified response type, and awaits the response.
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="request">The request message</param>
    /// <param name="timeout"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns>The response object</returns>
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
