using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Provides extension methods for future subscription.
/// </summary>
public static class FutureSubscriptionExtensions
{
    /// <summary>
    /// Sends message to subscriptions.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="subscriptions">The subscriptions value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> SendMessageToSubscriptionsAsync<T>(this BehaviorContext<FutureState> context,
        ContextMessageFactory<BehaviorContext<FutureState>, T> factory, IEnumerable<FutureSubscription> subscriptions, CancellationToken cancellationToken = default)
        where T : class
    {
        return await factory.UseAsync(context, async (ctx, s) =>
        {
            List<Task> tasks = subscriptions.Select(async sub =>
            {
                var endpoint = await context.GetSendEndpointAsync(sub.Address, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (sub.RequestId.HasValue)
                {
                    var pipe = new FutureResultPipe<T>(s.Pipe, sub.RequestId.Value);

                    await endpoint.SendAsync(s.Message, pipe, context.CancellationToken).ConfigureAwait(false);
                }
                else
                    await endpoint.SendAsync(s.Message, s.Pipe, context.CancellationToken).ConfigureAwait(false);
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return s.Message;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends message to subscriptions.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="factory">The factory value.</param>
    /// <param name="subscriptions">The subscriptions value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static async Task<T> SendMessageToSubscriptionsAsync<TInput, T>(this BehaviorContext<FutureState, TInput> context,
        ContextMessageFactory<BehaviorContext<FutureState, TInput>, T> factory, IEnumerable<FutureSubscription> subscriptions, CancellationToken cancellationToken = default)
        where TInput : class
        where T : class
    {
        return await factory.UseAsync(context, async (ctx, s) =>
        {
            List<Task> tasks = subscriptions.Select(async sub =>
            {
                var endpoint = await context.GetSendEndpointAsync(sub.Address, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (sub.RequestId.HasValue)
                {
                    var pipe = new FutureResultPipe<T>(s.Pipe, sub.RequestId.Value);

                    await endpoint.SendAsync(s.Message, pipe, context.CancellationToken).ConfigureAwait(false);
                }
                else
                    await endpoint.SendAsync(s.Message, s.Pipe, context.CancellationToken).ConfigureAwait(false);
            }).ToList();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return s.Message;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
