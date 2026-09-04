using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

public class BytesMessageDataReader<T> :
    IMessageDataReader<T>
{
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)new GetMessageData<byte[]>(address, repository, MessageDataConverter.ByteArray, cancellationToken);
    }
}
