// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.PropertyProviders
{
    using System;
    using System.Threading;
    using Converters;
    using Values;


    public class BytesMessageDataReader<T> :
        IMessageDataReader<T>
    {
        public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
        {
            return (MessageData<T>)new GetMessageData<byte[]>(address, repository, MessageDataConverter.ByteArray, cancellationToken);
        }
    }
}
