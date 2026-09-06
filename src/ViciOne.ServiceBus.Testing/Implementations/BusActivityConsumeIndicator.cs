using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>Tracks bus activity consume activity.</summary>
public class BusActivityConsumeIndicator : BaseBusActivityIndicatorConnectable,
    ISignalResource,
    IConsumeObserver
{
    readonly ISignalResource? _signalResource;
    int _messagesInFlight;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="signalResource">The signal resource.</param>
    public BusActivityConsumeIndicator(ISignalResource? signalResource)
    {
        _signalResource = signalResource;
    }

    /// <summary>Initializes a new instance.</summary>
    public BusActivityConsumeIndicator()
        :
        this(null)
    {
    }

    /// <summary>Gets a value indicating whether met.</summary>
    public override bool IsMet => Interlocked.CompareExchange(ref _messagesInFlight, int.MinValue, int.MinValue) == 0;

    Task IConsumeObserver.PreConsumeAsync<T>(ConsumeContext<T> context)
    {
        Interlocked.Increment(ref _messagesInFlight);
        return Task.CompletedTask;
    }

    Task IConsumeObserver.PostConsumeAsync<T>(ConsumeContext<T> context)
    {
        if (Interlocked.Decrement(ref _messagesInFlight) == 0)
            Signal();
        return Task.CompletedTask;
    }

    Task IConsumeObserver.ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception)
    {
        if (Interlocked.Decrement(ref _messagesInFlight) == 0)
            Signal();
        return Task.CompletedTask;
    }

    /// <summary>Signals the configured condition.</summary>
    public void Signal()
    {
        _signalResource?.Signal();
        ConditionUpdatedAsync();
    }
}
