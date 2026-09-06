using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Reads object message data values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ObjectMessageDataReader<T> :
    IMessageDataReader<T>
{
    readonly IMessageDataConverter<T> _converter;

    /// <summary>Initializes a new instance.</summary>
    public ObjectMessageDataReader()
    {
        _converter = new SystemTextJsonObjectMessageDataConverter<T>(ServiceBusMetadataJson.Options);
    }

    /// <summary>Gets message data.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message data.</returns>
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return new GetMessageData<T>(address, repository, _converter, cancellationToken);
    }
}
