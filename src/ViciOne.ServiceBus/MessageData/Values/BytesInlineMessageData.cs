using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Serialization;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Provides an immutable inline snapshot of binary message data.</summary>
internal sealed class BytesInlineMessageData :
    MessageData<byte[]>,
    IInlineMessageData
{
    readonly byte[] _value;

    /// <summary>Creates an inline snapshot with an optional repository address.</summary>
    /// <param name="value">The bytes to copy.</param>
    /// <param name="address">The repository address, when the same bytes were also stored.</param>
    public BytesInlineMessageData(byte[] value, Uri? address = null)
    {
        Address = address;
        _value = value is null ? throw new ArgumentNullException(nameof(value)) : [.. value];
    }

    /// <inheritdoc />
    public void Set(IMessageDataReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        reference.Text = default;
        reference.Data = [.. _value];
    }

    /// <inheritdoc />
    public Uri? Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Gets a new copy of the inline bytes.</summary>
    public Task<byte[]?> Value => Task.FromResult<byte[]?>([.. _value]);
}


/// <summary>Lazily deserializes an object from an immutable inline binary snapshot.</summary>
/// <typeparam name="T">The deserialized value type.</typeparam>
internal sealed class BytesInlineMessageData<T> :
    MessageData<T>,
    IInlineMessageData
{
    readonly IMessageDataConverter<T> _converter;
    readonly byte[] _value;
    readonly Lazy<Task<T?>> _valueTask;

    /// <summary>Creates a lazily converted inline snapshot with an optional repository address.</summary>
    /// <param name="converter">The converter used for the binary representation.</param>
    /// <param name="value">The bytes to copy.</param>
    /// <param name="address">The repository address, when the same bytes were also stored.</param>
    public BytesInlineMessageData(IMessageDataConverter<T> converter, byte[] value, Uri? address = null)
    {
        Address = address;
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        _value = value is null ? throw new ArgumentNullException(nameof(value)) : [.. value];

        _valueTask = new Lazy<Task<T?>>(GetValueAsync);
    }

    /// <inheritdoc />
    public void Set(IMessageDataReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        reference.Text = default;
        reference.Data = [.. _value];
    }

    /// <inheritdoc />
    public Uri? Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Gets the value deserialized once from the inline bytes.</summary>
    public Task<T?> Value => _valueTask.Value;

    async Task<T?> GetValueAsync()
    {
        using var stream = new MemoryStream(_value, false);

        return await _converter.ConvertAsync(stream, CancellationToken.None).ConfigureAwait(false);
    }
}
