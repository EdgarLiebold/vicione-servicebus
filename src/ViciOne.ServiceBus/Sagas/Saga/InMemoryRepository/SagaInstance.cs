using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Represents an instance of saga.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaInstance<TSaga> :
    IEquatable<SagaInstance<TSaga>>
    where TSaga : class, ISaga
{
    readonly SemaphoreSlim _inUse;
    readonly CancellationTokenSource _removal;
    readonly object _stateLock;
    bool _isRemoved;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="instance">The instance.</param>
    public SagaInstance(TSaga instance)
    {
        Instance = instance;
        _inUse = new SemaphoreSlim(1, 1);
        _removal = new CancellationTokenSource();
        _stateLock = new object();
    }

    /// <summary>Gets the instance.</summary>
    public TSaga Instance { get; }

    /// <summary>Gets a value indicating whether the instance has been removed from its repository.</summary>
    public bool IsRemoved
    {
        get
        {
            lock (_stateLock)
                return _isRemoved;
        }
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(SagaInstance<TSaga>? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualityComparer<TSaga>.Default.Equals(Instance, other.Instance);
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="obj">The obj.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return EqualityComparer<TSaga>.Default.GetHashCode(Instance);
    }

    /// <summary>Marks in use.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>Releases the owned resource.</summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>Removes the selected value.</summary>
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
