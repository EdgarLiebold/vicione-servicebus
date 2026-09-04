using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Futures;

public interface IRoutingSlipExecutor<in TInput>
    where TInput : class
{
    bool TrackRoutingSlip { set; }
    Task ExecuteAsync(BehaviorContext<FutureState, TInput> context, CancellationToken cancellationToken = default);
}
