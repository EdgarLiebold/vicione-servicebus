using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

public class ObjectMessageDataReader<T> :
    IMessageDataReader<T>
{
    readonly IMessageDataConverter<T> _converter;

    public ObjectMessageDataReader()
    {
        _converter = new SystemTextJsonObjectMessageDataConverter<T>(ServiceBusMetadataJson.Options);
    }

    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return new GetMessageData<T>(address, repository, _converter, cancellationToken);
    }
}
