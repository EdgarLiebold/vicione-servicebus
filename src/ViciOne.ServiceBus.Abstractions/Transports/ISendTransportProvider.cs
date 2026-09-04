using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for send transport provider.
/// </summary>
public interface ISendTransportProvider
{
    /// <summary>
    /// Gets send transport.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<ISendTransport> GetSendTransportAsync(Uri address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the normalize address operation.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    Uri NormalizeAddress(Uri address);
}
