using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface IExecuteActivityScopeProvider<TActivity, TArguments> :
    IProbeSite
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    ValueTask<IExecuteScopeContext<TArguments>> GetScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);

    ValueTask<IExecuteActivityScopeContext<TActivity, TArguments>> GetActivityScopeAsync(ExecuteContext<TArguments> context, CancellationToken cancellationToken = default);
}
