using System;
using System.Diagnostics;
using System.Linq;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Represents a composite event status value.
/// </summary>
[Serializable]
[DebuggerDisplay("{Status}")]
public struct CompositeEventStatus :
    IComparable<CompositeEventStatus>
{
    int _bits;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="bits">The bits value.</param>
    public CompositeEventStatus(int bits)
    {
        _bits = bits;
    }

    /// <summary>
    /// Gets the status value.
    /// </summary>
    public string Status
    {
        get
        {
            var bits = _bits;
            return string.Join("", Enumerable.Range(0, 32).Select(x => (bits & (1 << x)) == 0 ? "0" : "1"));
        }
    }

    /// <summary>
    /// Gets the bits value.
    /// </summary>
    public int Bits => _bits;

    /// <summary>
    /// Compares this instance with the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns>The result of the operation.</returns>
    public int CompareTo(CompositeEventStatus other)
    {
        return other._bits - _bits;
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(CompositeEventStatus other)
    {
        return other._bits == _bits;
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
        if (obj.GetType() != typeof(CompositeEventStatus))
            return false;
        return Equals((CompositeEventStatus)obj);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override int GetHashCode()
    {
        return 0;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="flag">The flag value.</param>
    public void Set(int flag)
    {
        _bits |= flag;
    }

    /// <summary>
    /// Determines whether set.
    /// </summary>
    /// <param name="flag">The flag value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool IsSet(int flag)
    {
        return (_bits & flag) == flag;
    }
}
