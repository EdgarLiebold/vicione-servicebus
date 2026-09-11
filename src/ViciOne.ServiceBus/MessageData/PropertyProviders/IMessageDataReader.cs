using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageData.PropertyProviders;

/// <summary>Creates lazy message-data handles for one supported value category.</summary>
/// <typeparam name="T">The value type read from repository content.</typeparam>
internal interface IMessageDataReader<T>
{
    /// <summary>Creates a lazy handle for one repository address.</summary>
    /// <param name="repository">The repository that owns the address.</param>
    /// <param name="address">The address to load.</param>
    /// <param name="cancellationToken">The token captured for lazy loading.</param>
    /// <returns>A populated repository-backed handle.</returns>
    MessageData<T> GetMessageData(IMessageDataRepository repository, Uri address, CancellationToken cancellationToken);
}
