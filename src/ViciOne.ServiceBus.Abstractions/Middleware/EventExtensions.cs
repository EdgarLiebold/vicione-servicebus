using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Publishes typed notifications through event pipelines.</summary>
public static class EventExtensions
{
    /// <summary>Publishes an event with a context-scoped clock and cancellation token.</summary>
    /// <typeparam name="TEvent">The event contract type.</typeparam>
    /// <param name="pipe">The event pipeline.</param>
    /// <param name="message">The event to publish.</param>
    /// <param name="timeProvider">The clock used to timestamp the event context.</param>
    /// <param name="cancellationToken">The token that cancels event processing.</param>
    /// <returns>The asynchronous dispatch of the event through every configured event-pipeline stage.</returns>
    public static Task PublishEventAsync<TEvent>(this IPipe<EventContext> pipe, TEvent message, TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(pipe);
        ArgumentNullException.ThrowIfNull(message);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var context = new PublishEventContext<TEvent>(message, timeProvider ?? TimeProvider.System, cancellationToken);

        return pipe.SendAsync(context);
    }

    private sealed class PublishEventContext<TEvent> :
        BasePipeContext,
        EventContext<TEvent>
        where TEvent : class
    {
        public PublishEventContext(TEvent @event, TimeProvider timeProvider, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Event = @event;
            Timestamp = timeProvider.GetUtcNow();
            this.SetTimeProvider(timeProvider);
        }

        public DateTimeOffset Timestamp { get; }

        public TEvent Event { get; }
    }
}
