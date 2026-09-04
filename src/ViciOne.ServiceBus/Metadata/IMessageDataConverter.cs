using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for message data converter.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageDataConverter<T>
{
    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <param name="stream">The stream value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken);
}
