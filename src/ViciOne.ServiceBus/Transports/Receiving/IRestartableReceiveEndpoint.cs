using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports;
/// <summary>
/// Internal endpoint control used by policies that temporarily pause message delivery. A pause is
/// restartable and is deliberately distinct from the externally visible terminal stop operation.
/// </summary>
internal interface IRestartableReceiveEndpoint :
    IReceiveEndpoint
{
    /// <summary>Gets the endpoint-scoped diagnostic context used by the restart policy.</summary>
    ILogContext LogContext { get; }

    /// <summary>Stops the active transport while retaining the endpoint for a policy restart.</summary>
    /// <param name="cancellationToken">The token that cancels the pause operation.</param>
    /// <returns>A task that completes after the transport has stopped and endpoint resources have reset.</returns>
    Task PauseAsync(CancellationToken cancellationToken);

    /// <summary>Starts a new transport generation after a policy pause.</summary>
    /// <param name="cancellationToken">The token that cancels restart.</param>
    /// <returns>A task that produces the new endpoint handle.</returns>
    Task<IReceiveEndpointHandle> RestartAsync(CancellationToken cancellationToken);
}
