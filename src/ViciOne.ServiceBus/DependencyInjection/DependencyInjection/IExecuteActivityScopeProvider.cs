// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System.Threading.Tasks;


    public interface IExecuteActivityScopeProvider<TActivity, TArguments> :
        IProbeSite
        where TActivity : class, IExecuteActivity<TArguments>
        where TArguments : class
    {
        ValueTask<IExecuteScopeContext<TArguments>> GetScope(ExecuteContext<TArguments> context);

        ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScope(ExecuteContext<TArguments> context);
    }
}
