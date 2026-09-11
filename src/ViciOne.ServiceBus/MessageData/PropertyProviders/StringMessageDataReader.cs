using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Creates lazy handles that decode repository content as UTF-8 text.</summary>
/// <typeparam name="T">The factory-selected text type.</typeparam>
internal sealed class StringMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <inheritdoc />
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)(object)new GetMessageData<string>(address, repository, MessageDataConverter.String, cancellationToken);
    }
}
