using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>If present, can be used to move the <see cref="ReceiveContext" /> to the error queue.</summary>
public interface IErrorTransport
{
    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default);
}
