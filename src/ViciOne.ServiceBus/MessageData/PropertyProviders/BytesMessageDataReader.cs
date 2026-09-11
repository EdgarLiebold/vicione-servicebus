using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Creates lazy handles that copy repository content into binary snapshots.</summary>
/// <typeparam name="T">The factory-selected binary type.</typeparam>
internal sealed class BytesMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <inheritdoc />
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)(object)new GetMessageData<byte[]>(address, repository, MessageDataConverter.ByteArray, cancellationToken);
    }
}
