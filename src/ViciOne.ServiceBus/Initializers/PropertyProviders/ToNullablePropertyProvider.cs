using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Provides a to nullable property provider implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class ToNullablePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty?>
    where TInput : class
    where TProperty : struct
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    public ToNullablePropertyProvider(IPropertyProvider<TInput, TProperty> provider)
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
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty?>(cancellationToken: cancellationToken);

        Task<TProperty> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult<TProperty?>(propertyTask.Result);

        async Task<TProperty?> GetPropertyAsync()
        {
            return await propertyTask.ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}
