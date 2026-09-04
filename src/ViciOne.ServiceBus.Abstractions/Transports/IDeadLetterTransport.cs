using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// If present, can be used to move the <see cref="ReceiveContext" /> to the dead letter queue
/// </summary>
public interface IDeadLetterTransport
{
    /// <summary>
    /// Writes the message to the dead letter queue, adding the reason as a transport header
    /// </summary>
    /// <param name="context"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default);
}
