using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Provides a bytes inline message data implementation.
/// </summary>
public class BytesInlineMessageData :
    MessageData<byte[]>,
    IInlineMessageData
{
    readonly byte[] _value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="address">The address value.</param>
    public BytesInlineMessageData(byte[] value, Uri? address = null)
    {
        Address = address;
        _value = value;

        Value = Task.FromResult<byte[]?>(value);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="reference">The reference value.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = default;
        reference.Data = _value;
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri? Address { get; }

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<byte[]?> Value { get; }
}


/// <summary>
/// Provides a bytes inline message data implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class BytesInlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IMessageDataConverter<T> _converter;
    readonly byte[] _value;
    readonly Lazy<Task<T?>> _valueTask;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    /// <param name="value">The value.</param>
    /// <param name="address">The address value.</param>
    public BytesInlineMessageData(IMessageDataConverter<T> converter, byte[] value, Uri? address = null)
    {
        Address = address;
        _value = value;

        _valueTask = new Lazy<Task<T?>>(() => GetValueAsync());

        _converter = converter;
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="reference">The reference value.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = default;
        reference.Data = _value;
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri? Address { get; }

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value => _valueTask.Value;

    async Task<T?> GetValueAsync()
    {
        using var stream = new MemoryStream(_value, false);

        return await _converter.ConvertAsync(stream, CancellationToken.None).ConfigureAwait(false);
    }
}
