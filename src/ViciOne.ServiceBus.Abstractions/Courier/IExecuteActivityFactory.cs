using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// A factory that creates an execute activity and then invokes the pipe for the activity context
/// </summary>
/// <typeparam name="TArguments"></typeparam>
/// <typeparam name="TActivity"></typeparam>
public interface IExecuteActivityFactory<out TActivity, TArguments> :
    IProbeSite
    where TArguments : class
    where TActivity : class, IExecuteActivity<TArguments>
{
    /// <summary>
    /// Executes the activity context by passing it to the activity factory, which creates the activity
    /// and then invokes the next pipe with the combined activity context
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default);
}
