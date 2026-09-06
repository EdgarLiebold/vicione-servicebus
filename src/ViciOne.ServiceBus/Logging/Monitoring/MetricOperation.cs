using System;
using System.Threading;

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

    /// <summary>
    /// Performs the record exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Interlocked.CompareExchange(ref _exception, exception, null);
    }

    /// <summary>
    /// Performs the complete operation.
    /// </summary>
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
