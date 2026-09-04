using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>
/// Defines the contract for compensate activity factory.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface ICompensateActivityFactory<out TActivity, TLog> :
    IProbeSite
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    /// <summary>
    /// Performs the compensate operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default);
}
