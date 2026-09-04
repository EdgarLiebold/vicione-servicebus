using System.Threading;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for transport supervisor.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ITransportSupervisor<out T> :
    ISupervisor<T>
    where T : class, PipeContext
{
    /// <summary>
    /// Gets the consume stopping value.
    /// </summary>
    CancellationToken ConsumeStopping { get; }
    /// <summary>
    /// Gets the send stopping value.
    /// </summary>
    CancellationToken SendStopping { get; }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <typeparam name="TAgent">The t agent type.</typeparam>
    /// <param name="agent">The agent value.</param>
    void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <typeparam name="TAgent">The t agent type.</typeparam>
    /// <param name="agent">The agent value.</param>
    void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent;
}
