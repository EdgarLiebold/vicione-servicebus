using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Retains saga state and coordinates exclusive use and permanent invalidation.</summary>
/// <typeparam name="TSaga">The referenced saga state type.</typeparam>
public class SagaInstance<TSaga> :
    IEquatable<SagaInstance<TSaga>>
    where TSaga : class, ISaga
{
    readonly SemaphoreSlim _inUse;
    readonly CancellationTokenSource _removal;
    readonly object _stateLock;
    bool _isRemoved;

    /// <summary>Retains the required saga state and creates its exclusive-use lease.</summary>
    /// <param name="instance">The required saga state, retained without copying.</param>
    public SagaInstance(TSaga instance)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        _inUse = new SemaphoreSlim(1, 1);
        _removal = new CancellationTokenSource();
        _stateLock = new object();
    }

    /// <summary>Gets the exact saga state supplied at construction.</summary>
    public TSaga Instance { get; }

    /// <summary>Gets whether the instance has been invalidated for further lease acquisition.</summary>
    public bool IsRemoved
    {
        get
        {
            lock (_stateLock)
                return _isRemoved;
        }
    }

    /// <summary>Compares the retained saga states with their default equality comparer.</summary>
    /// <param name="other">The wrapper to compare, or <see langword="null" />.</param>
    /// <returns>Whether both wrappers retain equal saga states.</returns>
    public bool Equals(SagaInstance<TSaga>? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualityComparer<TSaga>.Default.Equals(Instance, other.Instance);
    }

    /// <summary>Compares a wrapper of the same runtime type by its retained saga state.</summary>
    /// <param name="obj">The object to compare, or <see langword="null" />.</param>
    /// <returns>Whether the supplied object is an equal wrapper of the same runtime type.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((SagaInstance<TSaga>)obj);
    }

    /// <summary>Gets the retained saga state's hash code from its default equality comparer.</summary>
    /// <returns>The saga state's current hash code.</returns>
    public override int GetHashCode()
    {
        return EqualityComparer<TSaga>.Default.GetHashCode(Instance);
    }

    /// <summary>Acquires exclusive use unless the caller cancels or the instance is invalidated.</summary>
    /// <param name="cancellationToken">The token that cancels waiting for exclusive use.</param>
    /// <returns>A task that completes with one lease requiring a matching <see cref="Release" />.</returns>
    public async Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (_isRemoved)
                throw CreateRemovedException();
        }

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _removal.Token);
        try
        {
            await _inUse.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_removal.IsCancellationRequested)
        {
            throw CreateRemovedException();
        }

        lock (_stateLock)
        {
            if (!_isRemoved)
                return;

            _inUse.Release();
        }

        throw CreateRemovedException();
    }

    /// <summary>Releases exactly one lease previously acquired by the caller.</summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>Invalidates the instance and rejects pending and future lease acquisitions.</summary>
    /// <remarks>Does not release the current owner's lease or remove dictionary membership. Repeated invalidation has no additional effect.</remarks>
    public void Remove()
    {
        lock (_stateLock)
        {
            if (_isRemoved)
                return;

            _isRemoved = true;
        }

        _removal.Cancel();
    }

    SagaInstanceRemovedException CreateRemovedException() =>
        new(typeof(TSaga), Instance.CorrelationId);
}
