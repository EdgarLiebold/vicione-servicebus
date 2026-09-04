using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Defines the contract for execute activity.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteActivity<in TArguments> :
    IExecuteActivity
    where TArguments : class
{
    /// <summary>
    /// Execute the activity
    /// </summary>
    /// <param name="context">The execution context</param>
    /// <returns>An execution result, created from the execution passed to the activity</returns>
    Task<ExecutionResult> ExecuteAsync(ExecuteContext<TArguments> context);
}


/// <summary>
/// Marker interface used to assist identification in IoC containers.
/// Not to be used directly as it does not contain the message type of the
/// consumer
/// </summary>
/// <remarks>
/// Not to be used directly by application code, for internal reflection only
/// </remarks>
public interface IExecuteActivity
{
}
