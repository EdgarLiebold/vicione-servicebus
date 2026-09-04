using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

public class ConsumerJobHandle<T> :
    JobHandle
    where T : class
{
    readonly ConsumeJobContext<T> _context;
    readonly TimeSpan _jobCancellationTimeout;

    public ConsumerJobHandle(ConsumeJobContext<T> context, Task task, TimeSpan jobCancellationTimeout)
    {
        _context = context;
        _jobCancellationTimeout = jobCancellationTimeout;
        JobTask = task;
    }

    public Guid JobId => _context.JobId;
    public Task JobTask { get; }

    public async Task CancelAsync(string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); if (_context.CancellationToken.IsCancellationRequested)
            return;

        _context.Cancel(reason);

        try
        {
            await JobTask.OrTimeoutAsync(_jobCancellationTimeout, _context.GetTimeProvider()).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
