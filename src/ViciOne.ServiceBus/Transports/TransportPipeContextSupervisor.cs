using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Supervises the lifecycle of transport pipe context.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TransportPipeContextSupervisor<T> :
    PipeContextSupervisor<T>,
    ITransportSupervisor<T>
    where T : class, PipeContext
{
    readonly ISupervisor _consumeSupervisor;
    readonly ISupervisor _sendSupervisor;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    protected TransportPipeContextSupervisor(IPipeContextFactory<T> factory)
        : base(factory)
    {
        _consumeSupervisor = new Supervisor();
        _sendSupervisor = new Supervisor();
    }

    /// <summary>Gets the consume stopping.</summary>
    public CancellationToken ConsumeStopping => _consumeSupervisor.Stopping;
    /// <summary>Gets the send stopping.</summary>
    public CancellationToken SendStopping => _sendSupervisor.Stopping;

    /// <summary>Adds send agent to the configuration.</summary>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="agent">The agent.</param>
    public void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        _sendSupervisor.Add(agent);
    }

    /// <summary>Adds consume agent to the configuration.</summary>
    /// <typeparam name="TAgent">The agent type.</typeparam>
    /// <param name="agent">The agent.</param>
    public void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        _consumeSupervisor.Add(agent);
    }

    /// <summary>Stops supervisor.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        await _consumeSupervisor.StopAsync(context).ConfigureAwait(false);

        await _sendSupervisor.StopAsync(context).ConfigureAwait(false);

        await base.StopSupervisorAsync(context).ConfigureAwait(false);
    }
}
