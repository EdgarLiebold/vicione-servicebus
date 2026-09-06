using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Provides extension methods for agent.</summary>
public static class AgentExtensions
{
    /// <summary>Stop the agent, using the default StopContext.</summary>
    /// <param name="agent">The agent.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task StopAsync(this IAgent agent, CancellationToken cancellationToken = default)
    {
        var stopContext = new DefaultStopContext(cancellationToken);

        return agent.StopAsync(stopContext, cancellationToken: cancellationToken);
    }

    /// <summary>Stop the agent, using the default StopContext.</summary>
    /// <param name="agent">The agent.</param>
    /// <param name="reason">The reason for stopping the agent.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static Task StopAsync(this IAgent agent, string reason, CancellationToken cancellationToken = default)
    {
        var stopContext = new DefaultStopContext(cancellationToken) { Reason = reason };

        return agent.StopAsync(stopContext, cancellationToken: cancellationToken);
    }


    class DefaultStopContext :
        BasePipeContext,
        StopContext
    {
        public DefaultStopContext(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            Reason = "Stopped";
        }

        public string Reason { get; set; }
    }
}
