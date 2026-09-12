using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates an execution activity and invokes the pipeline bound to that instance.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
public interface IExecuteActivityFactory<out TActivity, TArguments> :
    IProbeSite
    where TArguments : class
    where TActivity : class, IExecuteActivity<TArguments>
{
    /// <summary>
    /// Creates an activity for the deserialized arguments and invokes its execution pipeline.
    /// </summary>
    /// <param name="context">The routing-slip execution context.</param>
    /// <param name="next">The pipeline that receives the activity-bound execution context.</param>
    /// <param name="cancellationToken">The token that cancels activity creation before it starts.</param>
    /// <returns>A task that completes after the activity pipeline and owned lifetime have completed.</returns>
    Task ExecuteAsync(ExecuteContext<TArguments> context, IPipe<ExecuteActivityContext<TActivity, TArguments>> next, CancellationToken cancellationToken = default);
}
