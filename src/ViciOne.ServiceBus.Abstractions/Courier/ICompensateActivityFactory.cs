using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

public interface ICompensateActivityFactory<out TActivity, TLog> :
    IProbeSite
    where TLog : class
    where TActivity : class, ICompensateActivity<TLog>
{
    Task CompensateAsync(CompensateContext<TLog> context, IPipe<CompensateActivityContext<TActivity, TLog>> next, CancellationToken cancellationToken = default);
}
