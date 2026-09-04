using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>
/// Provides a property converter property provider implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
/// <typeparam name="TInputProperty">The t input property type.</typeparam>
public class PropertyConverterPropertyProvider<TInput, TProperty, TInputProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TInputProperty> _converter;
    readonly IPropertyProvider<TInput, TInputProperty> _inputProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="converter">The converter value.</param>
    /// <param name="inputProvider">The input provider value.</param>
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
        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<TInputProperty?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (inputTask.Status == TaskStatus.RanToCompletion)
            return _converter.ConvertAsync(context, inputTask.Result, cancellationToken: cancellationToken);

        async Task<TProperty?> GetPropertyAsync()
        {
            var inputValue = await inputTask.ConfigureAwait(false);

            return await _converter.ConvertAsync(context, inputValue, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}
