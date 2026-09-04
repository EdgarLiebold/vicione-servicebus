using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

public class FromNullablePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
    where TProperty : struct
{
    readonly IPropertyProvider<TInput, TProperty?> _provider;

    public FromNullablePropertyProvider(IPropertyProvider<TInput, TProperty?> provider)
    {
        _provider = provider;
    }

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
