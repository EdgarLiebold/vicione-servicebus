using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Advanced.Registration;

/// <summary>Bind is used to store types bound to their owner, such as an IBusControl to an IMyBus.</summary>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The bound type.</typeparam>
public class Bind<TKey, TValue> :
    IEquatable<Bind<TKey, TValue>>
    where TValue : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="value">The value to process.</param>
    public Bind(TValue value)
    {
        Value = value;
    }

    /// <summary>Gets the value.</summary>
    public TValue Value { get; }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="other">The other.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(Bind<TKey, TValue>? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return EqualityComparer<TValue>.Default.Equals(Value, other.Value);
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
        return Equals((Bind<TKey, TValue>)obj);
    }

    /// <summary>Gets hash code.</summary>
    /// <returns>The hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return EqualityComparer<TValue>.Default.GetHashCode(Value);
    }

    /// <summary>Creates the requested value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <returns>The newly created instance.</returns>
    public static Bind<TKey, TValue, T> Create<T>(T value)
        where T : class
    {
        return new Bind<TKey, TValue, T>(value);
    }
}


/// <summary>Associates a registered service with a keyed binding.</summary>
/// <typeparam name="TKey1">The key1 type.</typeparam>
/// <typeparam name="TKey2">The key2 type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class Bind<TKey1, TKey2, TValue>
    where TValue : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="value">The value to process.</param>
    public Bind(TValue value)
    {
        Value = value;
    }

    /// <summary>Gets the value.</summary>
    public TValue Value { get; }
}


/// <summary>Associates a registered service with a keyed binding.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
public static class Bind<TKey>
{
    /// <summary>Creates the requested value.</summary>
    /// <typeparam name="TValue">The value stored by the member.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <returns>The newly created instance.</returns>
    public static Bind<TKey, TValue> Create<TValue>(TValue value)
        where TValue : class
    {
        return new Bind<TKey, TValue>(value);
    }
}
