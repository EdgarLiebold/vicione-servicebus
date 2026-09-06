using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Provides from nullable property services.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class FromNullablePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
    where TProperty : struct
{
    readonly IPropertyProvider<TInput, TProperty?> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public FromNullablePropertyProvider(IPropertyProvider<TInput, TProperty?> provider)
    {
        _provider = provider;
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<TProperty> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<TProperty?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
            return Task.FromResult(propertyTask.Result ?? default);

        async Task<TProperty> GetPropertyAsync()
        {
            return await propertyTask.ConfigureAwait(false) ?? default;
        }

        return GetPropertyAsync();
    }
}
