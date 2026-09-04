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
    ILogContext LogContext { get; }

    Task PauseAsync(CancellationToken cancellationToken);
    Task<ReceiveEndpointHandle> RestartAsync(CancellationToken cancellationToken);
}
