using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Executes work on stable hash partitions so work sharing a partition key remains serialized while
/// independent partitions can progress concurrently.
/// </summary>
public interface IPartitionedTaskExecutor<in TPartition> :
    IAsyncDisposable
{
    /// <summary>
    /// Performs the enqueue operation.
    /// </summary>
    /// <param name="partition">The partition value.</param>
    /// <param name="method">The method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task EnqueueAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="partition">The partition value.</param>
    /// <param name="method">The method value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExecuteAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
}
