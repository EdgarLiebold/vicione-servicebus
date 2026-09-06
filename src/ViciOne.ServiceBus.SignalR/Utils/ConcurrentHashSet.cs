using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ViciOne.ServiceBus.SignalR.Utils;

/// <summary>Stores a unique set of concurrent hash values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConcurrentHashSet<T> : IDisposable
{
    readonly HashSet<T> _hashSet;
    readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    /// <summary>Initializes a new instance.</summary>
    public ConcurrentHashSet()
    {
        _hashSet = new HashSet<T>();
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="equalityComparer">The equality comparer.</param>
    public ConcurrentHashSet(IEqualityComparer<T> equalityComparer)
    {
        _hashSet = new HashSet<T>(equalityComparer);
    }

    /// <summary>Gets the count.</summary>
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

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        if (_lock != null)
            _lock.Dispose();
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="item">The item.</param>
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

    /// <summary>Removes every item from the current collection.</summary>
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

    /// <summary>Determines whether the current collection contains the supplied value.</summary>
    /// <param name="item">The item.</param>
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

    /// <summary>Removes the selected value.</summary>
    /// <param name="item">The item.</param>
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

    /// <summary>Converts this value to array.</summary>
    /// <returns>The converted array.</returns>
    public T[] ToArray()
    {
        try
        {
            _lock.EnterReadLock();
            return _hashSet.ToArray();
        }
        finally
        {
            if (_lock.IsReadLockHeld)
                _lock.ExitReadLock();
        }
    }
}
