using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Builds and executes a routing slip for a future input.</summary>
/// <typeparam name="TInput">The future event contract used to build the routing slip.</typeparam>
internal interface IRoutingSlipExecutor<in TInput>
    where TInput : class
{
    /// <summary>Sets whether the routing slip remains pending until its terminal event is consumed.</summary>
    bool TrackRoutingSlip { set; }
    /// <summary>Builds and executes the routing slip for the supplied future event.</summary>
    /// <param name="context">The future event context used to build and execute the routing slip.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(IBehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default);
}
