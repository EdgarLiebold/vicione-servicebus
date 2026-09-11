using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.MessageData.Converters;

namespace ViciOne.ServiceBus.MessageData.Values;

/// <summary>Lazily loads and converts one repository-backed message-data value.</summary>
/// <typeparam name="T">The exposed value type.</typeparam>
internal sealed class GetMessageData<T> :
    MessageData<T>
{
    readonly CancellationToken _cancellationToken;
    readonly IMessageDataConverter<T> _converter;
    readonly IMessageDataRepository _repository;
    readonly Lazy<Task<T?>> _value;

    /// <summary>Creates a lazily loaded value for one repository address.</summary>
    /// <param name="address">The repository address to load.</param>
    /// <param name="repository">The repository that owns the address.</param>
    /// <param name="converter">The converter that reads the stored representation.</param>
    /// <param name="cancellationToken">The token that cancels loading and conversion.</param>
    public GetMessageData(Uri address, IMessageDataRepository repository, IMessageDataConverter<T> converter, CancellationToken cancellationToken)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _converter = converter ?? throw new ArgumentNullException(nameof(converter));

        _cancellationToken = cancellationToken;

        _value = new Lazy<Task<T?>>(GetValueAsync);
    }

    /// <inheritdoc />
    public Uri Address { get; }

    /// <inheritdoc />
    public bool HasValue => true;

    /// <summary>Gets the single cached load operation for this repository address.</summary>
    public Task<T?> Value => _value.Value;

    async Task<T?> GetValueAsync()
    {
        Stream? valueStream = null;
        try
        {
            valueStream = await _repository.GetAsync(Address, _cancellationToken).ConfigureAwait(false)
                ?? throw new MessageDataException($"The message-data repository returned no stream for address '{Address}'.");
            T? value = await _converter.ConvertAsync(valueStream, _cancellationToken).ConfigureAwait(false);
            if (_converter.TransfersSourceStreamOwnership)
                valueStream = null;

            return value;
        }
        finally
        {
            if (valueStream is not null)
                await valueStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
