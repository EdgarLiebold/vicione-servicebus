using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Creates lazy handles that deserialize repository content as JSON objects.</summary>
/// <typeparam name="T">The object contract type.</typeparam>
internal sealed class ObjectMessageDataReader<T> :
    IMessageDataReader<T>
{
    readonly IMessageDataConverter<T> _converter;

    /// <summary>Creates a reader with the service bus metadata serializer options.</summary>
    public ObjectMessageDataReader()
    {
        _converter = new SystemTextJsonObjectMessageDataConverter<T>(ServiceBusMetadataJson.Options);
    }

    /// <inheritdoc />
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return new GetMessageData<T>(address, repository, _converter, cancellationToken);
    }
}
