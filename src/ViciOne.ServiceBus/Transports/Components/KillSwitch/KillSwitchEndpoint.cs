using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Logging;

namespace ViciOne.ServiceBus.Transports.Components;
/// <summary>
/// Minimal runtime capability required by the kill switch. Keeping endpoint orchestration behind
/// this boundary prevents the state machine from depending on unrelated send, publish and probe APIs.
/// </summary>
internal interface IKillSwitchEndpoint
{
    object Identity { get; }
    Uri InputAddress { get; }
    ILogContext LogContext { get; }

    void ConnectConsumeObserver(IConsumeObserver observer);
    Task PauseAsync(CancellationToken cancellationToken);
    Task<ReceiveEndpointHandle> RestartAsync(CancellationToken cancellationToken);
}


internal sealed class RestartableReceiveEndpointKillSwitchEndpoint(IRestartableReceiveEndpoint endpoint) :
    IKillSwitchEndpoint
{
    public object Identity => endpoint;
    public Uri InputAddress => endpoint.InputAddress;
    public ILogContext LogContext => endpoint.LogContext;

    public void ConnectConsumeObserver(IConsumeObserver observer) => endpoint.ConnectConsumeObserver(observer);

    public Task PauseAsync(CancellationToken cancellationToken) => endpoint.PauseAsync(cancellationToken);

    public Task<ReceiveEndpointHandle> RestartAsync(CancellationToken cancellationToken) => endpoint.RestartAsync(cancellationToken);
}
