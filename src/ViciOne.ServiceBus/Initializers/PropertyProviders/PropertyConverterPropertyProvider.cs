using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Resolves an input property and converts it to the requested property type.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TInputProperty">The input property type.</typeparam>
internal sealed class PropertyConverterPropertyProvider<TInput, TProperty, TInputProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TInputProperty> _converter;
    readonly IPropertyProvider<TInput, TInputProperty> _inputProvider;

    /// <summary>Creates a provider that converts values returned by <paramref name="inputProvider" />.</summary>
    /// <param name="converter">The conversion applied to each resolved value.</param>
    /// <param name="inputProvider">The provider that resolves the source value.</param>
    public PropertyConverterPropertyProvider(IPropertyConverter<TProperty, TInputProperty>? converter,
        IPropertyProvider<TInput, TInputProperty>? inputProvider)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        if (inputProvider == null)
            throw new ArgumentNullException(nameof(inputProvider));

        _converter = converter;
        _inputProvider = inputProvider;
    }

    /// <summary>Forwards caller cancellation and observes each accepted source and conversion operation to completion.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for value resolution.</param>
    /// <param name="cancellationToken">The token forwarded to source resolution and conversion.</param>
    /// <returns>A task containing the converted property value.</returns>
    public async Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasInput)
            return default;

        Task<TInputProperty?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The input property provider returned null.");
        var inputValue = await inputTask.ConfigureAwait(false);
        Task<TProperty?> conversionTask = _converter.ConvertAsync(context, inputValue, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The property converter returned null.");
        return await conversionTask.ConfigureAwait(false);
    }
}
