using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>
/// Defines the contract for routing slip executor.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IRoutingSlipExecutor<in TInput>
    where TInput : class
{
    /// <summary>
    /// Gets or sets the track routing slip value.
    /// </summary>
    bool TrackRoutingSlip { set; }
    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default);
}
