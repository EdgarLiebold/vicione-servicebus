using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Provides standard stop requests for agent shutdown.</summary>
public static class AgentExtensions
{
    /// <summary>Stops an agent with the standard stop reason.</summary>
    /// <param name="agent">The agent to stop.</param>
    /// <param name="cancellationToken">The token that bounds the stop attempt.</param>
    /// <returns>The shared shutdown operation for the agent lifecycle.</returns>
    public static Task StopAsync(this IAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var stopContext = new DefaultStopContext(cancellationToken);

        return agent.StopAsync(stopContext, cancellationToken: cancellationToken);
    }

    /// <summary>Stops an agent with an explicit reason.</summary>
    /// <param name="agent">The agent to stop.</param>
    /// <param name="reason">The nonempty reason for stopping.</param>
    /// <param name="cancellationToken">The token that bounds the stop attempt.</param>
    /// <returns>The shared shutdown operation for the agent lifecycle.</returns>
    public static Task StopAsync(this IAgent agent, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var stopContext = new DefaultStopContext(cancellationToken, reason);

        return agent.StopAsync(stopContext, cancellationToken: cancellationToken);
    }


    sealed class DefaultStopContext :
        BasePipeContext,
        StopContext
    {
        public DefaultStopContext(CancellationToken cancellationToken, string reason = "Stop requested")
            : base(cancellationToken)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
