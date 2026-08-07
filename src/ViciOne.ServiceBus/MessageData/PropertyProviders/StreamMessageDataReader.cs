// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.PropertyProviders
{
    using System;
    using System.IO;
    using System.Threading;
    using Converters;
    using Values;


    public class StreamMessageDataReader<T> :
        IMessageDataReader<T>
    {
        public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
        {
            return (MessageData<T>)new GetMessageData<Stream>(address, repository, MessageDataConverter.Stream, cancellationToken);
        }
    }
}
