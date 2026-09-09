using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware.Partitioning;

/// <summary>Serializes pipeline operations assigned to the same partition.</summary>
internal sealed class Partition :
    IDisposable
{
    readonly int _index;
    readonly SemaphoreSlim _limit;
    long _attemptCount;
    long _failureCount;
    long _successCount;

    /// <summary>Creates a serialized partition with a diagnostic index.</summary>
    /// <param name="index">The zero-based partition index.</param>
    public Partition(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _index = index;
        _limit = new SemaphoreSlim(1);
    }

    /// <summary>Releases the semaphore after the owning partitioner has drained all operations.</summary>
    public void Dispose()
    {
        _limit.Dispose();
    }

    /// <summary>Reports admission attempts and downstream completion outcomes for this partition.</summary>
    /// <param name="context">The probe that receives the partition counters.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var partitionScope = context.CreateScope($"partition-{_index}");
        partitionScope.Set(new
        {
            AttemptCount = Interlocked.Read(ref _attemptCount),
            SuccessCount = Interlocked.Read(ref _successCount),
            FailureCount = Interlocked.Read(ref _failureCount)
        });
    }

    /// <summary>Serializes a context behind operations already accepted by this partition.</summary>
    /// <typeparam name="T">The pipeline context type.</typeparam>
    /// <param name="context">The context to execute.</param>
    /// <param name="next">The downstream pipeline to invoke after admission.</param>
    /// <param name="cancellationToken">An additional token that cancels only the admission wait.</param>
    /// <returns>The serialized operation, including its admission wait and downstream pipeline execution.</returns>
    public async Task SendAsync<T>(T context, IPipe<T> next, CancellationToken cancellationToken = default)
        where T : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        CancellationToken contextCancellationToken = context.CancellationToken;
        using CancellationTokenSource? linkedCancellation = CreateAdmissionCancellation(
            context.CancellationToken,
            cancellationToken,
            out CancellationToken admissionToken);
        var admitted = false;
        try
        {
            try
            {
                await _limit.WaitAsync(admissionToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(exception.Message, exception, cancellationToken);
            }
            catch (OperationCanceledException exception) when (contextCancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(exception.Message, exception, contextCancellationToken);
            }

            admitted = true;
            Interlocked.Increment(ref _attemptCount);

            try
            {
                await next.SendAsync(context).ConfigureAwait(false);
                Interlocked.Increment(ref _successCount);
            }
            catch
            {
                Interlocked.Increment(ref _failureCount);
                throw;
            }
        }
        finally
        {
            if (admitted)
                _limit.Release();
        }
    }

    static CancellationTokenSource? CreateAdmissionCancellation(
        CancellationToken contextCancellationToken,
        CancellationToken cancellationToken,
        out CancellationToken admissionToken)
    {
        if (!contextCancellationToken.CanBeCanceled || contextCancellationToken == cancellationToken)
        {
            admissionToken = cancellationToken;
            return null;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            admissionToken = contextCancellationToken;
            return null;
        }

        CancellationTokenSource linkedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(contextCancellationToken, cancellationToken);
        admissionToken = linkedCancellation.Token;
        return linkedCancellation;
    }
}
