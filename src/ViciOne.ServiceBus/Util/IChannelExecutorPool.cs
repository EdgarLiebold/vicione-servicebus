// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Util
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public interface IChannelExecutorPool<in TPartition> :
        IAsyncDisposable
    {
        Task Push(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
        Task Run(TPartition partition, Func<Task> method, CancellationToken cancellationToken = default);
    }
}
