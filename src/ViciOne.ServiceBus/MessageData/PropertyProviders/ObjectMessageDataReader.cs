using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>
/// Provides an object message data reader implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ObjectMessageDataReader<T> :
    IMessageDataReader<T>
{
    readonly IMessageDataConverter<T> _converter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ObjectMessageDataReader()
    {
        _converter = new SystemTextJsonObjectMessageDataConverter<T>(ServiceBusMetadataJson.Options);
    }

    /// <summary>
    /// Gets message data.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return new GetMessageData<T>(address, repository, _converter, cancellationToken);
    }
}
