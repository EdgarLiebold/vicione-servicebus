using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>
/// Provides a saga instance implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaInstance<TSaga> :
    IEquatable<SagaInstance<TSaga>>
    where TSaga : class, ISaga
{
    readonly SemaphoreSlim _inUse;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    public SagaInstance(TSaga instance)
    {
        Instance = instance;
        _inUse = new SemaphoreSlim(1);
    }

    /// <summary>
    /// Gets the instance value.
    /// </summary>
    public TSaga Instance { get; }

    /// <summary>
    /// Gets or sets the is removed value.
    /// </summary>
    public bool IsRemoved { get; set; }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(SagaInstance<TSaga>? other)
    {
        if (ReferenceEquals(null, other))
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return EqualityComparer<TSaga>.Default.Equals(Instance, other.Instance);
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="obj">The obj value.</param>
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

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return EqualityComparer<TSaga>.Default.GetHashCode(Instance);
    }

    /// <summary>
    /// Performs the mark in use operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        if (IsRemoved)
            throw new InvalidOperationException($"The saga instance was removed: {TypeCache<TSaga>.ShortName}: {Instance.CorrelationId}");

        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    public void Release()
    {
        if (IsRemoved)
            return;

        _inUse.Release();
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    public void Remove()
    {
        IsRemoved = true;
        _inUse.Release();
    }
}
