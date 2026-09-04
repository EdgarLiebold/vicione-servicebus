using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides a batch entry implementation.
/// </summary>
/// <typeparam name="TEntry">The t entry type.</typeparam>
public class BatchEntry<TEntry>
{
    readonly TaskCompletionSource<bool> _completed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    public BatchEntry(TEntry entry)
    {
        Entry = entry;
        _completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Gets the entry value.
    /// </summary>
    public TEntry Entry { get; }

    /// <summary>
    /// Gets the completed value.
    /// </summary>
    public Task Completed => _completed.Task;

    /// <summary>
    /// Sets completed.
    /// </summary>
    public void SetCompleted()
    {
        _completed.TrySetResult(true);
    }

    /// <summary>
    /// Sets faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void SetFaulted(Exception exception)
    {
        _completed.TrySetException(exception);
    }
}
