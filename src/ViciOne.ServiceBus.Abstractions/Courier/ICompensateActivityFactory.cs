// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System.Threading.Tasks;


    public interface ICompensateActivityFactory<out TActivity, TLog> :
        IProbeSite
        where TLog : class
        where TActivity : class, ICompensateActivity<TLog>
    {
        Task Compensate(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next);
    }
}
