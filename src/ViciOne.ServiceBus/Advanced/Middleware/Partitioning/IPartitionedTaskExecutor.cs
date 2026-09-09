using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Maps work onto stable hash partitions. Each partition has its own bounded worker pool; execution
/// is serialized within a partition when its concurrent-delivery limit is one.
/// </summary>
/// <typeparam name="TPartition">The value from which a worker-partition key is derived.</typeparam>
public interface IPartitionedTaskExecutor<in TPartition> :
    IAsyncDisposable
{
    /// <summary>Transfers a delegate to the bounded queue selected by its partition.</summary>
    /// <param name="partition">The value used to select a stable execution partition.</param>
    /// <param name="method">The delegate whose execution ownership transfers to the executor.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    /// <returns>A task that completes when the selected queue accepts the delegate.</returns>
    Task EnqueueAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);

    /// <summary>Executes a delegate on the queue selected by its partition and waits for completion.</summary>
    /// <param name="partition">The value used to select a stable execution partition.</param>
    /// <param name="method">The delegate to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the delegate begins.</param>
    /// <returns>A task that completes with the delegate.</returns>
    Task ExecuteAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
}
