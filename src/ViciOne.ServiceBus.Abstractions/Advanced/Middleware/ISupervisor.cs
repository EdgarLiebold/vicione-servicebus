namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Defines a lifecycle that supervises a dynamic set of child agents.</summary>
public interface ISupervisor :
    IAgent
{
    /// <summary>Gets the highest number of concurrently registered agents.</summary>
    int PeakActiveCount { get; }

    /// <summary>Gets the total number of agents accepted during this supervisor's lifetime.</summary>
    long TotalCount { get; }

    /// <summary>Adds an agent while the supervisor is accepting new work.</summary>
    /// <param name="agent">The child agent to supervise.</param>
    /// <exception cref="ArgumentNullException"><paramref name="agent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The agent does not expose its readiness or completion task.</exception>
    /// <exception cref="InvalidOperationException">The supervisor has begun stopping.</exception>
    void Add(IAgent agent);
}


/// <summary>Combines a supervisor lifecycle with a source for a pipe context.</summary>
/// <typeparam name="TContext">The pipe context exposed by the supervisor.</typeparam>
public interface ISupervisor<out TContext> :
    ISupervisor,
    IAgent<TContext>
    where TContext : class, PipeContext
{
}
