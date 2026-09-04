using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Copies the input property, as-is, for the property value
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TProperty"></typeparam>
public class DelegatePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _inputProvider;
    readonly Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> _valueProvider;

    public DelegatePropertyProvider(IPropertyProvider<TInput, TProperty> inputProvider,
        Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> valueProvider)
    {
        if (inputProvider == null)
            throw new ArgumentNullException(nameof(inputProvider));

        _inputProvider = inputProvider;
        _valueProvider = valueProvider;
    }

    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        if (!context.TryGetPayload(out TransformContext<TInput>? transformContext))
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<TProperty?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (inputTask.IsCompleted)
            return GetValueAsync(inputTask.Result);

        async Task<TProperty?> GetPropertyAsync()
        {
            var inputValue = await inputTask.ConfigureAwait(false);

            return await GetValueAsync(inputValue).ConfigureAwait(false);
        }

        async Task<TProperty?> GetValueAsync(TProperty? inputValue)
        {
            var propertyContext = new MessageTransformPropertyContext<TProperty, TInput>(transformContext, inputValue);

            return await _valueProvider(propertyContext).ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }
}
