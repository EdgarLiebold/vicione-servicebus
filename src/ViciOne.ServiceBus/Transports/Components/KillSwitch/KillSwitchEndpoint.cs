#nullable enable
namespace ViciOne.ServiceBus.Transports.Components;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;


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
    Task Pause(CancellationToken cancellationToken);
    Task<ReceiveEndpointHandle> Restart(CancellationToken cancellationToken);
}


internal sealed class RestartableReceiveEndpointKillSwitchEndpoint(IRestartableReceiveEndpoint endpoint) :
    IKillSwitchEndpoint
{
    public object Identity => endpoint;
    public Uri InputAddress => endpoint.InputAddress;
    public ILogContext LogContext => endpoint.LogContext;

    public void ConnectConsumeObserver(IConsumeObserver observer) => endpoint.ConnectConsumeObserver(observer);

    public Task Pause(CancellationToken cancellationToken) => endpoint.Pause(cancellationToken);

    public Task<ReceiveEndpointHandle> Restart(CancellationToken cancellationToken) => endpoint.Restart(cancellationToken);
}
