// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Futures
{
    using System.Threading.Tasks;


    public interface IRoutingSlipExecutor<in TInput>
        where TInput : class
    {
        bool TrackRoutingSlip { set; }
        Task Execute(BehaviorContext<FutureState, TInput> context);
    }
}
