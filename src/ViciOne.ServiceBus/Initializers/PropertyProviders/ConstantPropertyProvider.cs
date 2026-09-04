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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyValue">The property value value.</param>
    public ConstantPropertyProvider(TProperty? propertyValue)
    {
        _propertyValue = Task.FromResult<TProperty?>(propertyValue);
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TProperty?>(cancellationToken); return _propertyValue;
    }
}
