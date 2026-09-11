using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Provides a populated value together with the repository address of its stored representation.</summary>
/// <typeparam name="T">The stored value type.</typeparam>
internal sealed class StoredMessageData<T> :
    MessageData<T>
{
    readonly T _value;

    /// <summary>Creates a repository-backed message-data value.</summary>
    /// <param name="address">The non-null repository address.</param>
    /// <param name="value">The non-null value represented by the stored bytes.</param>
    public StoredMessageData(Uri address, T value)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        ArgumentNullException.ThrowIfNull(value);
        _value = value is byte[] bytes
            ? (T)(object)bytes.AsSpan().ToArray()
            : value;
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Gets the stored value, returning an independent copy for binary data.</summary>
    public Task<T?> Value => Task.FromResult<T?>(_value is byte[] bytes
        ? (T)(object)bytes.AsSpan().ToArray()
        : _value);
}
