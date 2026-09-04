using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Provides extension methods for event.
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// Publishes event.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public static Task PublishEventAsync<T>(this IPipe<EventContext> pipe, T message, TimeProvider? timeProvider = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); if (pipe == null)
            throw new ArgumentNullException(nameof(pipe));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var context = new PublishEventContext<T>(message, timeProvider ?? TimeProvider.System);

        return pipe.SendAsync(context);
    }


    class PublishEventContext<T> :
        BasePipeContext,
        EventContext<T>
        where T : class
    {
        public PublishEventContext(T @event, TimeProvider timeProvider)
        {
            Event = @event;
            Timestamp = timeProvider.GetUtcNow();
            this.SetTimeProvider(timeProvider);
        }

        public DateTimeOffset Timestamp { get; }

        public T Event { get; }
    }
}
