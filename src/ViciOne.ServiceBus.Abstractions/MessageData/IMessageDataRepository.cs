using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Storage of large message data that can be stored and retrieved separate of the message body.
/// Implemented as a claim-check pattern when an identifier is stored in the message body which
/// is used to retrieve the message data separately.
/// </summary>
public interface IMessageDataRepository
{
    /// <summary>Returns a stream to read the message data for the specified address.</summary>
    /// <param name="address">The data address.</param>
    /// <param name="cancellationToken">A cancellation token for the request.</param>
    /// <returns>A task that produces the requested value.</returns>
    Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default);

    /// <summary>Puts message data into the repository.</summary>
    /// <param name="stream">The stream of data for the message.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the put outcome.</returns>
    Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default);
}
