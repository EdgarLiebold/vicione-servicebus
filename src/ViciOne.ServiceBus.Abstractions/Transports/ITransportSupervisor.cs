using System.Threading;

namespace ViciOne.ServiceBus.Transports;

public interface ITransportSupervisor<out T> :
    ISupervisor<T>
    where T : class, PipeContext
{
    CancellationToken ConsumeStopping { get; }
    CancellationToken SendStopping { get; }

    void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;

    void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;
}
