using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Defines the operations required by message data reader.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageDataReader<T>
{
    /// <summary>Gets message data.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The message data.</returns>
    MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken);
}
