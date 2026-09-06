using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Copies the input property, as-is, for the property value.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class VariablePropertyProvider<TInput, TProperty, TValue> :
    IPropertyProvider<TInput, TValue>
    where TInput : class
    where TProperty : class, IInitializerVariable<TValue>
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public VariablePropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider;
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<TValue?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TValue>(cancellationToken: cancellationToken);

        Task<TProperty?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
            return propertyTask.Result == null
                ? TaskResults.DefaultAsync<TValue>(cancellationToken: cancellationToken)
                : GetValueAsync(propertyTask.Result);

        async Task<TValue?> GetPropertyAsync()
        {
            var property = await propertyTask.ConfigureAwait(false);
            if (property == null)
                return default;

            return await property.GetValueAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        async Task<TValue?> GetValueAsync(TProperty property)
        {
            return await property.GetValueAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}
