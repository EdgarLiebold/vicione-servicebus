using System;
using System.Threading;

#nullable enable
namespace ViciOne.ServiceBus.Logging;
/// <summary>
/// Completes the metric observations associated with one message-flow operation. Telemetry
/// observers are outside the product trust boundary and cannot change message-flow semantics.
/// </summary>
public sealed class MetricOperation
{
    private Action<Exception?>? _complete;
    private Exception? _exception;

    internal MetricOperation(Action<Exception?> complete)
    {
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
    }

    public void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Interlocked.CompareExchange(ref _exception, exception, null);
    }

    public void Complete()
    {
        Action<Exception?>? complete = Interlocked.Exchange(ref _complete, null);
        if (complete == null)
            return;

        try
        {
            complete(Volatile.Read(ref _exception));
        }
        catch
        {
            // Application-owned observers never replace the operation outcome or exception.
        }
    }
}
