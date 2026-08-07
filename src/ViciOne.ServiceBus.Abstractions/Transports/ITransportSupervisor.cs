// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Transports
{
    using System.Threading;


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
}
