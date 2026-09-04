using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Copies the input property, as-is, for the property value
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class InputPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IReadProperty<TInput, TProperty> _inputProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    public InputPropertyProvider(PropertyInfo? propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TProperty>(propertyInfo);
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<TProperty?>(cancellationToken); return Task.FromResult(context.HasInput
                    ? _inputProperty.Get(context.Input)
                    : default);
    }
}
