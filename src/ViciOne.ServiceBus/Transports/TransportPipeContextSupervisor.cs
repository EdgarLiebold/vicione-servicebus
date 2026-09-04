using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a transport pipe context supervisor implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TransportPipeContextSupervisor<T> :
    PipeContextSupervisor<T>,
    ITransportSupervisor<T>
    where T : class, PipeContext
{
    readonly ISupervisor _consumeSupervisor;
    readonly ISupervisor _sendSupervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="factory">The factory value.</param>
    protected TransportPipeContextSupervisor(IPipeContextFactory<T> factory)
        : base(factory)
    {
        _consumeSupervisor = new Supervisor();
        _sendSupervisor = new Supervisor();
    }

    /// <summary>
    /// Gets the consume stopping value.
    /// </summary>
    public CancellationToken ConsumeStopping => _consumeSupervisor.Stopping;
    /// <summary>
    /// Gets the send stopping value.
    /// </summary>
    public CancellationToken SendStopping => _sendSupervisor.Stopping;

    /// <summary>
    /// Adds send agent to the configuration.
    /// </summary>
    /// <typeparam name="TAgent">The t agent type.</typeparam>
    /// <param name="agent">The agent value.</param>
    public void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        _sendSupervisor.Add(agent);
    }

    /// <summary>
    /// Adds consume agent to the configuration.
    /// </summary>
    /// <typeparam name="TAgent">The t agent type.</typeparam>
    /// <param name="agent">The agent value.</param>
    public void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        _consumeSupervisor.Add(agent);
    }

    /// <summary>
    /// Stops supervisor.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        await _consumeSupervisor.StopAsync(context).ConfigureAwait(false);

        await _sendSupervisor.StopAsync(context).ConfigureAwait(false);

        await base.StopSupervisorAsync(context).ConfigureAwait(false);
    }
}
