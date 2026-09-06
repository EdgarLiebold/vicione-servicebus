using System;
using System.IO;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Reads stream message data values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class StreamMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <summary>Gets message data.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message data.</returns>
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)new GetMessageData<Stream>(address, repository, MessageDataConverter.Stream, cancellationToken);
    }
}
