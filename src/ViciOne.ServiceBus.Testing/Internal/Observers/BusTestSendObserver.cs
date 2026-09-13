using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records send attempts and restarts inactivity detection around each attempt.</summary>
internal sealed class BusTestSendObserver :
    InactivityTestObserver,
    ISendObserver
{
    readonly SentMessageList _messages;

    /// <summary>Creates a send observer.</summary>
    /// <param name="timeout">The maximum time an assertion waits for a matching send.</param>
    /// <param name="inactivityTimeout">The interval without send activity that indicates inactivity.</param>
    /// <param name="testCompleted">The token that ends pending test observations.</param>
    /// <param name="timeProvider">The clock used for assertion timeouts, inactivity, and timestamps.</param>
    public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new SentMessageList(timeout, testCompleted, timeProvider);

        StartTimer(inactivityTimeout);
    }

    /// <summary>Gets the recorded send attempts.</summary>
    public ISentMessageList Messages => _messages;

    /// <summary>Marks send activity as in progress and restarts the inactivity interval.</summary>
    /// <typeparam name="TMessage">The sent message type.</typeparam>
    /// <param name="context">The send context entering the pipeline.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task PreSendAsync<TMessage>(SendContext<TMessage> context)
        where TMessage : class
    {
        return RestartTimerAsync();
    }

    /// <summary>Records a successful send and marks its activity as complete.</summary>
    /// <typeparam name="TMessage">The sent message type.</typeparam>
    /// <param name="context">The successfully sent message context.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task PostSendAsync<TMessage>(SendContext<TMessage> context)
        where TMessage : class
    {
        _messages.Add(context);

        return RestartTimerAsync(false);
    }

    /// <summary>Records a failed send and marks its activity as complete.</summary>
    /// <typeparam name="TMessage">The sent message type.</typeparam>
    /// <param name="context">The send context that faulted.</param>
    /// <param name="exception">The exception raised by the send pipeline.</param>
    /// <returns>A completed task after the timer has restarted.</returns>
    public Task SendFaultAsync<TMessage>(SendContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        _messages.Add(context, exception);

        return RestartTimerAsync(false);
    }
}
