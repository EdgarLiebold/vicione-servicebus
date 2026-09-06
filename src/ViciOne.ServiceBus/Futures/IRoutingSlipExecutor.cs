using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Defines the operations required by routing slip executor.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IRoutingSlipExecutor<in TInput>
    where TInput : class
{
    /// <summary>Gets or sets the track routing slip.</summary>
    bool TrackRoutingSlip { set; }
    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default);
}
