using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>
/// Bind is used to store types bound to their owner, such as an IBusControl to an IMyBus.
/// </summary>
/// <typeparam name="TKey">The key type</typeparam>
/// <typeparam name="TValue">The bound type</typeparam>
public class Bind<TKey, TValue> :
    IEquatable<Bind<TKey, TValue>>
    where TValue : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    public Bind(TValue value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Bind<TKey, TValue>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
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
        return Equals((Bind<TKey, TValue>)obj);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return EqualityComparer<TValue>.Default.GetHashCode(Value);
    }

    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public static Bind<TKey, TValue, T> Create<T>(T value)
        where T : class
    {
        return new Bind<TKey, TValue, T>(value);
    }
}


/// <summary>
/// Provides a bind implementation.
/// </summary>
/// <typeparam name="TKey1">The t key1 type.</typeparam>
/// <typeparam name="TKey2">The t key2 type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class Bind<TKey1, TKey2, TValue>
    where TValue : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    public Bind(TValue value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public TValue Value { get; }
}


/// <summary>
/// Provides a bind implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public static class Bind<TKey>
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <typeparam name="TValue">The t value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public static Bind<TKey, TValue> Create<TValue>(TValue value)
        where TValue : class
    {
        return new Bind<TKey, TValue>(value);
    }
}
