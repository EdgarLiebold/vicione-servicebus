using System;
using System.IO;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Creates lazy handles that transfer repository stream ownership to the caller.</summary>
/// <typeparam name="T">The factory-selected stream type.</typeparam>
internal sealed class StreamMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <inheritdoc />
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)(object)new GetMessageData<Stream>(address, repository, MessageDataConverter.Stream, cancellationToken);
    }
}
