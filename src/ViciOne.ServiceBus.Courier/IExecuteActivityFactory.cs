using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>A factory that creates an execute activity and then invokes the pipe for the activity context.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteActivityFactory<out TActivity, TArguments> :
    IProbeSite
    where TArguments : class
    where TActivity : class, IExecuteActivity<TArguments>
{
    /// <summary>
    /// Executes the activity context by passing it to the activity factory, which creates the activity
    /// and then invokes the next pipe with the combined activity context.
    /// </summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default);
}
