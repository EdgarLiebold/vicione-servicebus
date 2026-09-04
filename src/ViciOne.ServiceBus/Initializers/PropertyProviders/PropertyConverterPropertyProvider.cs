using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

public class PropertyConverterPropertyProvider<TInput, TProperty, TInputProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyConverter<TProperty, TInputProperty> _converter;
    readonly IPropertyProvider<TInput, TInputProperty> _inputProvider;

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
