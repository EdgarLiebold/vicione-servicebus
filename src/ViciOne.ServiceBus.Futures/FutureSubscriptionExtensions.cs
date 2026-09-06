using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.SagaStateMachine;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Delivers terminal future messages to registered subscribers.</summary>
public static class FutureSubscriptionExtensions
{
    /// <summary>Creates a terminal message from future state and sends it to every subscriber.</summary>
    /// <typeparam name="T">The terminal message contract.</typeparam>
    /// <param name="context">The future state context used to create and send the message.</param>
    /// <param name="factory">The terminal message factory.</param>
    /// <param name="subscriptions">The subscriber destinations and optional request identifiers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message delivered to all subscribers.</returns>
    public static async Task<T> SendMessageToSubscriptionsAsync<T>(this BehaviorContext<FutureState> context,
        ContextMessageFactory<BehaviorContext<FutureState>, T> factory, IEnumerable<FutureSubscription> subscriptions, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(subscriptions);
        cancellationToken.ThrowIfCancellationRequested();
        FutureSubscription[] subscriberSnapshot = subscriptions.ToArray();
        foreach (FutureSubscription subscription in subscriberSnapshot)
            ArgumentNullException.ThrowIfNull(subscription);

        return await factory.UseAsync(context, async (ctx, s) =>
        {
            Task[] tasks = subscriberSnapshot.Select(async subscription =>
            {
                var endpoint = await ctx.GetSendEndpointAsync(subscription.Address, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (subscription.RequestId.HasValue)
                {
                    var pipe = new FutureResultPipe<T>(s.Pipe, subscription.RequestId.Value);

                    await endpoint.SendAsync(s.Message, pipe, cancellationToken).ConfigureAwait(false);
                }
                else
                    await endpoint.SendAsync(s.Message, s.Pipe, cancellationToken).ConfigureAwait(false);
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return s.Message;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a terminal message from a future event and sends it to every subscriber.</summary>
    /// <typeparam name="TInput">The future event contract available to the message factory.</typeparam>
    /// <typeparam name="T">The terminal message contract.</typeparam>
    /// <param name="context">The future event context used to create and send the message.</param>
    /// <param name="factory">The terminal message factory.</param>
    /// <param name="subscriptions">The subscriber destinations and optional request identifiers.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message delivered to all subscribers.</returns>
    public static async Task<T> SendMessageToSubscriptionsAsync<TInput, T>(this BehaviorContext<FutureState, TInput> context,
        ContextMessageFactory<BehaviorContext<FutureState, TInput>, T> factory, IEnumerable<FutureSubscription> subscriptions, CancellationToken cancellationToken = default)
        where TInput : class
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(subscriptions);
        cancellationToken.ThrowIfCancellationRequested();
        FutureSubscription[] subscriberSnapshot = subscriptions.ToArray();
        foreach (FutureSubscription subscription in subscriberSnapshot)
            ArgumentNullException.ThrowIfNull(subscription);

        return await factory.UseAsync(context, async (ctx, s) =>
        {
            Task[] tasks = subscriberSnapshot.Select(async subscription =>
            {
                var endpoint = await ctx.GetSendEndpointAsync(subscription.Address, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (subscription.RequestId.HasValue)
                {
                    var pipe = new FutureResultPipe<T>(s.Pipe, subscription.RequestId.Value);

                    await endpoint.SendAsync(s.Message, pipe, cancellationToken).ConfigureAwait(false);
                }
                else
                    await endpoint.SendAsync(s.Message, s.Pipe, cancellationToken).ConfigureAwait(false);
            }).ToArray();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return s.Message;
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
