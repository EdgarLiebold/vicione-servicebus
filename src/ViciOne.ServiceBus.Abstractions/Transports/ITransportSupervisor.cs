using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by transport supervisor.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ITransportSupervisor<out T> :
    ISupervisor<T>
    where T : class, PipeContext
{
    /// <summary>Gets the consume stopping.</summary>
    CancellationToken ConsumeStopping { get; }
    /// <summary>Gets the send stopping.</summary>
    CancellationToken SendStopping { get; }

    /// <summary>Adds consume agent to the configuration.</summary>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="agent">The agent.</param>
    void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;

    /// <summary>Adds send agent to the configuration.</summary>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="agent">The agent.</param>
    void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;
}
