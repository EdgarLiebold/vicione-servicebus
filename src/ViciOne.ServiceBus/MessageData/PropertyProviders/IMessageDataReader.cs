using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

public interface IMessageDataReader<T>
{
    MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken);
}
