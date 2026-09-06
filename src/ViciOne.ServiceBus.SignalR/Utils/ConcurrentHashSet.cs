using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ViciOne.ServiceBus.SignalR.Utils;

// From here: https://stackoverflow.com/a/11034999/6558597
/// <summary>
/// Provides a concurrent hash set implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConcurrentHashSet<T> : IDisposable
{
    readonly HashSet<T> _hashSet;
    readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConcurrentHashSet()
    {
        _hashSet = new HashSet<T>();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="equalityComparer">The equality comparer value.</param>
    public ConcurrentHashSet(IEqualityComparer<T> equalityComparer)
    {
        _hashSet = new HashSet<T>(equalityComparer);
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count
    {
        get
        {
            try
            {
                _lock.EnterReadLock();
                return _hashSet.Count;
            }
            finally
            {
                if (_lock.IsReadLockHeld)
                    _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_lock != null)
            _lock.Dispose();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Add(T item)
    {
        try
        {
            _lock.EnterWriteLock();
            return _hashSet.Add(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Performs the clear operation.
    /// </summary>
    public void Clear()
    {
        try
        {
            _lock.EnterWriteLock();
            _hashSet.Clear();
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Performs the contains operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Contains(T item)
    {
        try
        {
            _lock.EnterReadLock();
            return _hashSet.Contains(item);
        }
        finally
        {
            if (_lock.IsReadLockHeld)
                _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Remove(T item)
    {
        try
        {
            _lock.EnterWriteLock();
            return _hashSet.Remove(item);
        }
        finally
        {
            if (_lock.IsWriteLockHeld)
                _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Performs the to array operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public T[] ToArray()
    {
        try
        {
            _lock.EnterReadLock();
            return _hashSet.ToArray(); // Internally Linq .ToArray uses CopyTo
        }
        finally
        {
            if (_lock.IsReadLockHeld)
                _lock.ExitReadLock();
        }
    }
}
