using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Coordinates readiness, completion, and shutdown for a dynamic set of child agents.</summary>
public class Supervisor :
    Agent,
    ISupervisor
{
    readonly Dictionary<long, IAgent> _agents;
    long _nextId;
    int _peakActiveCount;
    long _totalCount;

    /// <summary>Initializes an empty supervisor.</summary>
    public Supervisor()
    {
        _agents = new Dictionary<long, IAgent>();
    }

    /// <inheritdoc />
    public void Add(IAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _ = agent.Ready ?? throw new ArgumentException("The agent must expose a readiness task.", nameof(agent));
        Task completed = agent.Completed ?? throw new ArgumentException("The agent must expose a completion task.", nameof(agent));

        long id;
        lock (_agents)
        {
            if (IsStopping)
                throw new InvalidOperationException("The supervisor is stopping and cannot accept another agent.");

            id = ++_nextId;
            _agents.Add(id, agent);

            _totalCount++;
            var currentActiveCount = _agents.Count;

            if (currentActiveCount > _peakActiveCount)
                _peakActiveCount = currentActiveCount;

            SetReady();
        }

        void HandleAgentCompletion(Task task)
        {
            if (task.IsCompletedSuccessfully)
                Remove(id);
            else if (task.IsFaulted)
                _ = task.Exception;
        }

        completed.ContinueWith(HandleAgentCompletion, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <inheritdoc />
    public int PeakActiveCount => Volatile.Read(ref _peakActiveCount);

    /// <inheritdoc />
    public long TotalCount => Volatile.Read(ref _totalCount);

    /// <inheritdoc />
    public override void SetReady()
    {
        if (IsAlreadyReady)
            return;

        lock (_agents)
        {
            SetReady(_agents.Count == 0
                ? Task.CompletedTask
                : Task.WhenAll(_agents.Values.Select(x => x.Ready).ToArray()));
        }
    }

    /// <inheritdoc />
    protected override Task StopAgentAsync(StopContext context)
    {
        IAgent[] agents;
        lock (_agents)
        {
            agents = _agents.Count == 0
                ? []
                : _agents.Values.Where(x => !x.Completed.IsCompletedSuccessfully).ToArray();
        }

        return StopSupervisorAsync(new Context(context, agents));
    }

    /// <summary>Stops the captured child agents and waits for their lifecycle completion.</summary>
    /// <param name="context">The stop request and the child agents captured for this attempt.</param>
    /// <returns>A task that completes when every captured child has completed.</returns>
    protected virtual async Task StopSupervisorAsync(StopSupervisorContext context)
    {
        switch (context.Agents.Length)
        {
            case 0:
                SetCompleted(Task.CompletedTask);
                break;
            case 1:
                SetCompleted(context.Agents[0].Completed);

                await context.Agents[0].StopAsync(context).ConfigureAwait(false);
                break;
            case > 1:
                {
                    var completedTasks = new Task[context.Agents.Length];
                    for (var i = 0; i < context.Agents.Length; i++)
                        completedTasks[i] = context.Agents[i].Completed;

                    SetCompleted(Task.WhenAll(completedTasks));

                    var stopTasks = new Task[context.Agents.Length];
                    for (var i = 0; i < context.Agents.Length; i++)
                        stopTasks[i] = context.Agents[i].StopAsync(context);

                    await Task.WhenAll(stopTasks).ConfigureAwait(false);
                    break;
                }
        }

        await Completed.ConfigureAwait(false);
    }

    void Remove(long id)
    {
        lock (_agents)
            _agents.Remove(id);
    }


    sealed class Context :
        ProxyPipeContext,
        StopSupervisorContext
    {
        readonly StopContext _context;

        public Context(StopContext context, IAgent[] agents)
            : base(context)
        {
            _context = context;
            Agents = agents;
        }

        string StopContext.Reason => _context.Reason;

        public IAgent[] Agents { get; }
    }
}
