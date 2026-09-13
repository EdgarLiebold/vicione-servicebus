using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Executes one routing-slip activity.</summary>
/// <typeparam name="TArguments">The activity argument contract.</typeparam>
[ActivityContract]
public interface IExecuteActivity<in TArguments>
    where TArguments : class
{
    /// <summary>Executes the activity.</summary>
    /// <param name="context">The execution context.</param>
    /// <returns>A task containing the routing-slip outcome of the activity execution.</returns>
    Task<ExecutionResult> ExecuteAsync(ExecuteContext<TArguments> context);
}
