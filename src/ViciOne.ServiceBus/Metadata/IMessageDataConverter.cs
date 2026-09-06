using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Defines the operations required by message data converter.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageDataConverter<T>
{
    /// <summary>Converts the supplied value.</summary>
    /// <param name="stream">The stream.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
    Task<T?> ConvertAsync(Stream stream, CancellationToken cancellationToken);
}
