using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

public class TestConsumeObserver :
    IConsumeObserver
{
    readonly ReceivedMessageList _messages;

    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken)
        : this(timeout, inactivityToken, TimeProvider.System)
    {
    }

    public TestConsumeObserver(TimeSpan timeout, CancellationToken inactivityToken, TimeProvider timeProvider)
    {
        _messages = new ReceivedMessageList(timeout, inactivityToken, timeProvider);
    }

    public IReceivedMessageList Messages => _messages;

    Task IConsumeObserver.PreConsumeAsync<T>(ConsumeContext<T> context)
    {
        return Task.CompletedTask;
    }

    Task IConsumeObserver.PostConsumeAsync<T>(ConsumeContext<T> context)
    {
        _messages.Add(context);

        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        _messages.Add(context, exception);

        return Task.CompletedTask;
    }
}
