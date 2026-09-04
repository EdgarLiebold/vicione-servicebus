using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Copies the input property, as-is, for the property value
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class VariablePropertyProvider<TInput, TProperty, TValue> :
    IPropertyProvider<TInput, TValue>
    where TInput : class
    where TProperty : class, IInitializerVariable<TValue>
{
    readonly IPropertyProvider<TInput, TProperty> _provider;

    public VariablePropertyProvider(IPropertyProvider<TInput, TProperty> provider)
    {
        _provider = provider;
    }

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
