using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Applies send-pipeline configuration through the general message-contract path.</summary>
public interface ISendContextPipe
{
    /// <summary>Configures a typed send context without dispatching the message.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The send context receiving pipeline configuration.</param>
    /// <param name="cancellationToken">The token that cancels pipeline configuration.</param>
    /// <returns>A task that completes after the pipeline configuration has been applied.</returns>
    Task SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;
}
