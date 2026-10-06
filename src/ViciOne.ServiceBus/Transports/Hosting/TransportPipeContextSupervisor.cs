using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Supervises transport pipe contexts and separates send and consume child lifecycles.</summary>
/// <typeparam name="T">The transport pipe context type.</typeparam>
public abstract class TransportPipeContextSupervisor<T> :
    PipeContextSupervisor<T>,
    ITransportSupervisor<T>
    where T : class, PipeContext
{
    readonly ISupervisor _consumeSupervisor;
    readonly ISupervisor _sendSupervisor;

    /// <summary>Initializes the supervisor with the factory that owns transport contexts.</summary>
    /// <param name="factory">The transport context factory.</param>
    protected TransportPipeContextSupervisor(IPipeContextFactory<T> factory)
        : base(factory ?? throw new ArgumentNullException(nameof(factory)))
    {
        _consumeSupervisor = new Supervisor();
        _sendSupervisor = new Supervisor();
    }

    /// <summary>Gets the token signaled when consume agents are stopping.</summary>
    public CancellationToken ConsumeStopping => _consumeSupervisor.Stopping;
    /// <summary>Gets the token signaled when send agents are stopping.</summary>
    public CancellationToken SendStopping => _sendSupervisor.Stopping;

    /// <summary>Adds an agent to the send lifecycle.</summary>
    /// <typeparam name="TAgent">The send agent type.</typeparam>
    /// <param name="agent">The send agent to supervise.</param>
    public void AddSendAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        ArgumentNullException.ThrowIfNull(agent);
        _sendSupervisor.Add(agent);
    }

    /// <summary>Adds an agent to the consume lifecycle.</summary>
    /// <typeparam name="TAgent">The consume agent type.</typeparam>
    /// <param name="agent">The consume agent to supervise.</param>
    public void AddConsumeAgent<TAgent>(TAgent agent)
        where TAgent : IAgent
    {
        ArgumentNullException.ThrowIfNull(agent);
        _consumeSupervisor.Add(agent);
    }

    /// <summary>Stops consume agents, send agents, and then the owned transport contexts.</summary>
    /// <param name="context">The supervisor stop context.</param>
    /// <returns>A task that completes when every owned lifecycle has stopped.</returns>
    protected override async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        List<Exception>? failures = null;

        async Task StopPhaseAsync(Func<Task> stop)
        {
            Task? stopTask = null;
            try
            {
                stopTask = stop();
                await stopTask.ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                failures ??= new List<Exception>();
                if (stopTask?.Exception is AggregateException aggregate)
                    failures.AddRange(aggregate.InnerExceptions);
                else
                    failures.Add(failure);
            }
        }

        await StopPhaseAsync(() => _consumeSupervisor.StopAsync(context)).ConfigureAwait(false);
        await StopPhaseAsync(() => _sendSupervisor.StopAsync(context)).ConfigureAwait(false);
        await StopPhaseAsync(() => base.StopSupervisorAsync(context)).ConfigureAwait(false);

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException(failures);
    }
}
