using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>If present, can be used to move the <see cref="ReceiveContext" /> to the dead letter queue.</summary>
public interface IDeadLetterTransport
{
    /// <summary>Writes the message to the dead letter queue, adding the reason as a transport header.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default);
}
