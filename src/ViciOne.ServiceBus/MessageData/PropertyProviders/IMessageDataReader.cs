// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.MessageData.PropertyProviders
{
    using System;
    using System.Threading;


    public interface IMessageDataReader<T>
    {
        MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken);
    }
}
