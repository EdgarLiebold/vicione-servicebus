using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Carries bytes inline message data.</summary>
public class BytesInlineMessageData :
    MessageData<byte[]>,
    IInlineMessageData
{
    readonly byte[] _value;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="address">The address.</param>
    public BytesInlineMessageData(byte[] value, Uri? address = null)
    {
        Address = address;
        _value = value;

        Value = Task.FromResult<byte[]?>(value);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="reference">The reference.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = default;
        reference.Data = _value;
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address { get; }

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => true;

    /// <summary>Gets the value.</summary>
    public Task<byte[]?> Value { get; }
}


/// <summary>Carries bytes inline message data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class BytesInlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IMessageDataConverter<T> _converter;
    readonly byte[] _value;
    readonly Lazy<Task<T?>> _valueTask;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="address">The address.</param>
    public BytesInlineMessageData(IMessageDataConverter<T> converter, byte[] value, Uri? address = null)
    {
        Address = address;
        _value = value;

        _valueTask = new Lazy<Task<T?>>(() => GetValueAsync());

        _converter = converter;
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="reference">The reference.</param>
    public void Set(IMessageDataReference reference)
    {
        reference.Text = default;
        reference.Data = _value;
    }

    /// <summary>Gets the address.</summary>
    public Uri? Address { get; }

    /// <summary>Gets whether this instance contains a value.</summary>
    public bool HasValue => true;

    /// <summary>Gets the value.</summary>
    public Task<T?> Value => _valueTask.Value;

    async Task<T?> GetValueAsync()
    {
        using var stream = new MemoryStream(_value, false);

        return await _converter.ConvertAsync(stream, CancellationToken.None).ConfigureAwait(false);
    }
}
