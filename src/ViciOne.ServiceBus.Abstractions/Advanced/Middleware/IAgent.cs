using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Defines readiness, completion, and shutdown for a supervised component.</summary>
public interface IAgent
{
    /// <summary>Gets the task that completes when the agent is ready, or faults or is canceled if readiness cannot be reached.</summary>
    Task Ready { get; }

    /// <summary>Gets the task that reaches a terminal state when the agent's lifecycle ends.</summary>
    Task Completed { get; }

    /// <summary>
    /// Gets the token that is canceled when the first accepted stop attempt begins. Registered
    /// callbacks observe the transition and cannot fail the stop operation.
    /// </summary>
    CancellationToken Stopping { get; }

    /// <summary>
    /// Gets the token that is canceled when a stop attempt succeeds. Registered callbacks observe
    /// the transition and cannot change the successful outcome.
    /// </summary>
    CancellationToken Stopped { get; }

    /// <summary>
    /// Stops the agent and any components it supervises. Concurrent callers share the active attempt;
    /// a failed or canceled attempt can be retried. Stop failures are returned by this method and do not
    /// fault <see cref="Completed"/>.
    /// </summary>
    /// <param name="context">The reason and cancellation budget for the stop attempt.</param>
    /// <param name="cancellationToken">The token that cancels this caller's wait.</param>
    /// <returns>The active shared shutdown operation for this lifecycle.</returns>
    Task StopAsync(StopContext context, CancellationToken cancellationToken = default);
}


/// <summary>Combines a supervised lifecycle with a source for a pipe context.</summary>
/// <typeparam name="TContext">The pipe context exposed by the agent.</typeparam>
public interface IAgent<out TContext> :
    IAgent,
    IPipeContextSource<TContext>
    where TContext : class, PipeContext
{
}
