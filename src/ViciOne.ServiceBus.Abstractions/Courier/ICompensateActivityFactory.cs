using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Creates compensate activity instances.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivityFactory<out TActivity, TLog> :
    IProbeSite
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    /// <summary>Compensates the completed activity.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default);
}
