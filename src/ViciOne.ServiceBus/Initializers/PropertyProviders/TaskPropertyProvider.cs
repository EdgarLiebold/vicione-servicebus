using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

public class TaskPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, Task<TProperty?>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    public TaskPropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider;
    }

    public Task<Task<TProperty?>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult<Task<TProperty?>?>(context.HasInput
            ? _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            : TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken));
    }
}
