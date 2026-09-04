using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class BusTestConsumeObserver :
    InactivityTestObserver,
    IConsumeObserver
{
    readonly ReceivedMessageList _messages;
    int _activeCount;

    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted)
        : this(timeout, testCompleted, TimeProvider.System)
    {
    }

    public BusTestConsumeObserver(TimeSpan timeout, CancellationToken testCompleted, TimeProvider timeProvider)
        : base(timeProvider)
    {
        _messages = new ReceivedMessageList(timeout, testCompleted, timeProvider);
    }

    public IReceivedMessageList Messages => _messages;

    public override bool IsInactive => _activeCount == 0;

    public Task PreConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        Interlocked.Increment(ref _activeCount);

        return Task.CompletedTask;
    }

    public Task PostConsumeAsync<T>(ConsumeContext<T> context)
        where T : class
    {
        _messages.Add(context);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }

    public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
        where T : class
    {
        _messages.Add(context, exception);

        return Interlocked.Decrement(ref _activeCount) == 0 ? NotifyInactiveAsync() : Task.CompletedTask;
    }
}
