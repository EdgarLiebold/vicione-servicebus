using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Records completed and faulted consumptions and reports when no consumption remains active.</summary>
internal sealed class BusTestConsumeObserver :
    InactivityTestObserver,
    IConsumeObserver
{
    readonly ConsumedMessageList _messages;
    int _activeCount;

    /// <summary>Creates a consume observer.</summary>
    /// <param name="timeout">The maximum time an assertion waits for a matching consumption.</param>
    /// <param name="testCompleted">The token that ends pending test observations.</param>
    /// <param name="timeProvider">The clock used for assertion timeouts and observation timestamps.</param>
    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new ConsumedMessageList(timeout, testCompleted, timeProvider);
    }

    /// <summary>Gets the recorded consumption attempts.</summary>
    public IConsumedMessageList Messages => _messages;

    /// <summary>Gets whether every started consumption has reached a terminal callback.</summary>
    public override bool IsInactive => Volatile.Read(ref _activeCount) == 0;

    /// <summary>Marks a consumption as active.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The consume context entering the pipeline.</param>
    /// <returns>A completed task after the active count has been updated.</returns>
    public Task PreConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        Interlocked.Increment(ref _activeCount);

        return Task.CompletedTask;
    }

    /// <summary>Records a successful consumption and marks it as no longer active.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The successfully consumed message context.</param>
    /// <returns>A task that completes after any resulting inactivity notification.</returns>
    public Task PostConsumeAsync<TMessage>(ConsumeContext<TMessage> context)
        where TMessage : class
    {
        _messages.Add(context);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }

    /// <summary>Records a failed consumption and marks it as no longer active.</summary>
    /// <typeparam name="TMessage">The consumed message type.</typeparam>
    /// <param name="context">The consume context that faulted.</param>
    /// <param name="exception">The exception raised by the consume pipeline.</param>
    /// <returns>A task that completes after any resulting inactivity notification.</returns>
    public Task ConsumeFaultAsync<TMessage>(ConsumeContext<TMessage> context, Exception exception)
        where TMessage : class
    {
        _messages.Add(context, exception);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }
}
