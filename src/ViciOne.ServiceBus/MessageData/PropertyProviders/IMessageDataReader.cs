using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>
/// Defines the contract for message data reader.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageDataReader<T>
{
    /// <summary>
    /// Gets message data.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken);
}
