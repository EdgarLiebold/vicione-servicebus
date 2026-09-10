using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Computes a transformed property value from its current value and message context.</summary>
/// <typeparam name="TInput">The source message type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class DelegatePropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
{
    readonly IPropertyProvider<TInput, TProperty> _inputProvider;
    readonly Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> _valueProvider;

    /// <summary>Creates a provider that transforms the current value resolved by another property provider.</summary>
    /// <param name="inputProvider">The provider that resolves the current source-property value.</param>
    /// <param name="valueProvider">The transform applied to the current value and message context.</param>
    public DelegatePropertyProvider(IPropertyProvider<TInput, TProperty> inputProvider,
        Func<TransformPropertyContext<TProperty, TInput>, Task<TProperty>> valueProvider)
    {
        _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
        _valueProvider = valueProvider ?? throw new ArgumentNullException(nameof(valueProvider));
    }

    /// <summary>Resolves and transforms the property when a source transform context is available.</summary>
    /// <typeparam name="TMessage">The message being initialized.</typeparam>
    /// <param name="context">The initialization context that carries the source transform.</param>
    /// <param name="cancellationToken">The token used to cancel property resolution.</param>
    /// <returns>A task containing the transformed property value, or the default value when no source input is available.</returns>
    public Task<TProperty?> GetPropertyAsync<TMessage>(InitializeContext<TMessage, TInput> context,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (!context.TryGetPayload(out TransformContext<TInput>? transformContext))
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        if (!context.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        Task<TProperty?> inputTask = _inputProvider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (inputTask.IsCompletedSuccessfully)
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
