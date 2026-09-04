using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Returns a constant value for the property
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class ConstantPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly Task<TProperty?> _propertyValue;

    public ConstantPropertyProvider(TProperty? propertyValue)
    {
        _propertyValue = Task.FromResult<TProperty?>(propertyValue);
    }

    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TProperty?>(cancellationToken); return _propertyValue;
    }
}
