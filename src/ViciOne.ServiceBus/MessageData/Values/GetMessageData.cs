using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>
/// Gets the message data when accessed via Value, using the specified repository and converter.
/// </summary>
/// <typeparam name="T">The message data property type</typeparam>
public class GetMessageData<T> :
    MessageData<T>
{
    readonly CancellationToken _cancellationToken;
    readonly IMessageDataConverter<T> _converter;
    readonly IMessageDataRepository _repository;
    readonly Lazy<Task<T?>> _value;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="repository">The repository value.</param>
    /// <param name="converter">The converter value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public GetMessageData(Uri address, IMessageDataRepository repository, IMessageDataConverter<T> converter, CancellationToken cancellationToken)
    {
        Address = address;
        _repository = repository;
        _converter = converter;

        _cancellationToken = cancellationToken;

        _value = new Lazy<Task<T?>>(GetValueAsync);
    }

    /// <summary>
    /// Gets the address value.
    /// </summary>
    public Uri Address { get; }

    /// <summary>
    /// Gets the has value value.
    /// </summary>
    public bool HasValue => true;

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public Task<T?> Value => _value.Value;

    async Task<T?> GetValueAsync()
    {
        // To prevent the stream message data convertor from having to copy the stream, the stream
        // is not disposed if the converter is a StreamMessageDataConverter

        Stream? valueStream = null;
        try
        {
            valueStream = await _repository.GetAsync(Address, _cancellationToken).ConfigureAwait(false);
            return await _converter.ConvertAsync(valueStream, _cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_converter.GetType() != typeof(StreamMessageDataConverter))
                valueStream?.Dispose();
        }
    }
}
