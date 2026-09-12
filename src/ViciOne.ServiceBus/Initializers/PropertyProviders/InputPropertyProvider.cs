using System;
using System.Reflection;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Reads one input property without conversion.</summary>
/// <typeparam name="TInput">The input object type.</typeparam>
/// <typeparam name="TProperty">The input property type.</typeparam>
internal sealed class InputPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IReadProperty<TInput, TProperty> _inputProperty;

    /// <summary>Creates a provider for a readable input property.</summary>
    /// <param name="propertyInfo">The readable input property.</param>
    public InputPropertyProvider(PropertyInfo? propertyInfo)
    {
        if (propertyInfo == null)
            throw new ArgumentNullException(nameof(propertyInfo));

        _inputProperty = ReadPropertyCache<TInput>.GetProperty<TProperty>(propertyInfo);
    }

    /// <summary>Reads the configured property when the context contains an input.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for the read.</param>
    /// <param name="cancellationToken">The token that cancels property access.</param>
    /// <returns>A task containing the input value, or the default value when no input is available.</returns>
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<TProperty?>(cancellationToken);

        return Task.FromResult(context.HasInput
            ? _inputProperty.Get(context.Input)
            : default);
    }
}
