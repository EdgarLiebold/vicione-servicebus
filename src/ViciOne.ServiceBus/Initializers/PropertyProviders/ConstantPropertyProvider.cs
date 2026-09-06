using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Returns a constant value for the property.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ConstantPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly Task<TProperty?> _propertyValue;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyValue">The property value.</param>
    public ConstantPropertyProvider(TProperty? propertyValue)
    {
        _propertyValue = Task.FromResult<TProperty?>(propertyValue);
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TProperty?>(cancellationToken); return _propertyValue;
    }
}
