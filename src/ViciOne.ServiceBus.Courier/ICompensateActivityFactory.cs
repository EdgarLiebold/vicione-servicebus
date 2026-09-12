using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates a compensation activity and invokes the pipeline bound to that instance.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The compensation-log contract.</typeparam>
public interface ICompensateActivityFactory<out TActivity, TLog> :
    IProbeSite
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    /// <summary>Creates an activity for the deserialized log and invokes its compensation pipeline.</summary>
    /// <param name="context">The routing-slip compensation context.</param>
    /// <param name="next">The pipeline that receives the activity-bound compensation context.</param>
    /// <param name="cancellationToken">The token that cancels activity creation before it starts.</param>
    /// <returns>A task that completes after the activity pipeline and owned lifetime have completed.</returns>
    Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default);
}
