using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Copies the input property, as-is, for the property value.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class DelegatePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _inputProvider;
    readonly Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> _valueProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="inputProvider">The input provider.</param>
    /// <param name="valueProvider">The value provider.</param>
    public DelegatePropertyProvider(IPropertyProvider<TInput, TProperty> inputProvider,
        Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> valueProvider)
    {
        if (inputProvider == null)
            throw new ArgumentNullException(nameof(inputProvider));

        _inputProvider = inputProvider;
        _valueProvider = valueProvider;
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
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
