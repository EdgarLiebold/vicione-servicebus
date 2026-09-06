using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Reads bytes message data values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class BytesMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <summary>Gets message data.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message data.</returns>
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)new GetMessageData<byte[]>(address, repository, MessageDataConverter.ByteArray, cancellationToken);
    }
}
