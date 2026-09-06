using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Caching.Implementation;

internal sealed class PendingResourceCreation<TValue>
    where TValue : class
{
    public PendingResourceCreation(ResourceCacheIndexBase<TValue> index, object requestedKey, CancellationToken lifetimeCancellationToken)
    {
        Index = index;
        RequestedKey = requestedKey;
        Completion = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        OwnershipReleased = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CreationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellationToken);
    }

    public ResourceCacheIndexBase<TValue> Index { get; }
    public object RequestedKey { get; }
    public TaskCompletionSource<TValue> Completion { get; }
    public TaskCompletionSource OwnershipReleased { get; }
    public CancellationTokenSource CreationCancellationSource { get; }
    public Task? Runner { get; set; }
    public bool Invalidated { get; set; }
}
