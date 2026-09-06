using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Provides send transport services.</summary>
public interface ISendTransportProvider
{
    /// <summary>Gets send transport.</summary>
    /// <param name="address">The address.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default);

    /// <summary>Normalizes address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The uri produced by the operation.</returns>
    Uri NormalizeAddress(Uri address);
}
