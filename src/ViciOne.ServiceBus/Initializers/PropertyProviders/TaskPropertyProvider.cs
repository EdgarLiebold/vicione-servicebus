using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Provides task property services.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class TaskPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, Task<TProperty?>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public TaskPropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider;
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<Task<TProperty?>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult<Task<TProperty?>?>(context.HasInput
            ? _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            : TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken));
    }
}
