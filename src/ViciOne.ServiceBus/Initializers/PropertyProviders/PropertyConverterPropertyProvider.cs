using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Provides property converter property services.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TInputProperty">The input property type.</typeparam>
public class PropertyConverterPropertyProvider<TInput, TProperty, TInputProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TInputProperty> _converter;
    readonly IPropertyProvider<TInput, TInputProperty> _inputProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="converter">The converter.</param>
    /// <param name="inputProvider">The input provider.</param>
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

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
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
