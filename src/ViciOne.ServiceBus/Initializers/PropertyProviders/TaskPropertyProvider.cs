using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Provides a task property provider implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class TaskPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, Task<TProperty?>>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public TaskPropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Gets property.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<Task<TProperty?>?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        return Task.FromResult<Task<TProperty?>?>(context.HasInput
            ? _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            : TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken));
    }
}
