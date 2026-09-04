using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for publish transport provider.
/// </summary>
public interface IPublishTransportProvider
{
    /// <summary>
    /// Gets publish transport.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="publishAddress">The publish address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class;
}
