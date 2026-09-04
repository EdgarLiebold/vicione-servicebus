using System;
using System.Threading;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>
/// Provides a string message data reader implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class StringMessageDataReader<T> :
    IMessageDataReader<T>
{
    /// <summary>
    /// Gets message data.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken)
    {
        return (MessageData<T>)new GetMessageData<string>(address, repository, MessageDataConverter.String, cancellationToken);
    }
}
