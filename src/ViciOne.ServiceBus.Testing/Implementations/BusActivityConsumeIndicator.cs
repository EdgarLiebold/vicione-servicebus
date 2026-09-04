using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Implementations;

/// <summary>
/// Provides a bus activity consume indicator implementation.
/// </summary>
public class BusActivityConsumeIndicator : BaseBusActivityIndicatorConnectable,
    ISignalResource,
    IConsumeObserver
{
    readonly ISignalResource? _signalResource;
    int _messagesInFlight;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="signalResource">The signal resource value.</param>
    public BusActivityConsumeIndicator(ISignalResource? signalResource)
    {
        _signalResource = signalResource;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public BusActivityConsumeIndicator()
        :
        this(null)
    {
    }

    /// <summary>
    /// Gets the is met value.
    /// </summary>
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

    /// <summary>
    /// Performs the signal operation.
    /// </summary>
    public void Signal()
    {
        _signalResource?.Signal();
        ConditionUpdatedAsync();
    }
}
