using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides publish transport services.</summary>
public interface IPublishTransportProvider
{
    /// <summary>Gets publish transport.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="publishAddress">The publish address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class;
}
