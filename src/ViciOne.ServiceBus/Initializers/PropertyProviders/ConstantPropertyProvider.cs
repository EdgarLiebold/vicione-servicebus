using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Returns the same configured value for every initialization.</summary>
/// <typeparam name="TInput">The accepted input-object type.</typeparam>
/// <typeparam name="TProperty">The constant value type.</typeparam>
internal sealed class ConstantPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly Task<TProperty?> _propertyValue;

    /// <summary>Creates a provider for <paramref name="propertyValue" />.</summary>
    /// <param name="propertyValue">The value returned by the provider.</param>
    public ConstantPropertyProvider(TProperty? propertyValue)
    {
        _propertyValue = Task.FromResult<TProperty?>(propertyValue);
    }

    /// <summary>Returns the configured constant.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The current initialization context.</param>
    /// <param name="cancellationToken">The token that cancels value resolution.</param>
    /// <returns>A task containing the configured value.</returns>
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TProperty?>(cancellationToken);

        return _propertyValue;
    }
}
