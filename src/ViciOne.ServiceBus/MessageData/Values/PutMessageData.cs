using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Defers message-data storage until the outgoing message is transformed.</summary>
/// <typeparam name="T">The value type to store.</typeparam>
internal sealed class PutMessageData<T> :
    MessageData<T>
{
    readonly T _value;

    /// <summary>Creates a populated value whose storage policy is applied during send processing.</summary>
    /// <param name="value">The non-null value to store.</param>
    public PutMessageData(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value is byte[] bytes)
            value = (T)(object)bytes.AsSpan().ToArray();

        _value = value;
    }

    /// <inheritdoc />
    public Uri? Address => null;

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Gets the deferred value, returning an independent copy for binary data.</summary>
    public Task<T?> Value => Task.FromResult<T?>(_value is byte[] bytes
        ? (T)(object)bytes.AsSpan().ToArray()
        : _value);
}
