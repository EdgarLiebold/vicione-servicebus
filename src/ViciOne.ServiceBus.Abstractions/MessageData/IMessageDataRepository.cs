using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Stores and retrieves message payload data addressed by a claim-check URI.</summary>
public interface IMessageDataRepository
{
    /// <summary>Opens stored message data for reading.</summary>
    /// <param name="address">The repository address returned by <see cref="PutAsync" />.</param>
    /// <param name="cancellationToken">Cancels retrieval.</param>
    /// <returns>A task containing a readable stream owned by the caller.</returns>
    Task<Stream> GetAsync(Uri address, CancellationToken cancellationToken = default);

    /// <summary>Stores the remaining content of a readable stream.</summary>
    /// <param name="stream">The source stream, whose ownership remains with the caller.</param>
    /// <param name="timeToLive">The optional retention period for the stored data.</param>
    /// <param name="cancellationToken">Cancels storage.</param>
    /// <returns>A task containing the repository address assigned to the stored data.</returns>
    Task<Uri> PutAsync(Stream stream, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default);
}
