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
    Task EnqueueAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
    Task ExecuteAsync(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
}
