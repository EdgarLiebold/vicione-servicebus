using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Converts transform property values.</summary>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class TransformPropertyConverter<TProperty> :
    IPropertyConverter<TProperty, TProperty>
    where TProperty : class
{
    readonly IMessageInitializer<TProperty> _initializer;

    /// <summary>Creates a converter backed by the nested property's message initializer.</summary>
    /// <param name="initializer">The initializer that transforms non-null property values.</param>
    public TransformPropertyConverter(IMessageInitializer<TProperty> initializer)
    {
        _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
    }

    /// <summary>Transforms a non-null nested property while preserving its parent message context.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The parent message initialization context.</param>
    /// <param name="input">The nested source property, or <see langword="null" /> when no transform can be applied.</param>
    /// <param name="cancellationToken">The token used to cancel nested initialization.</param>
    /// <returns>A task containing the transformed property, or the default value when no source transform is available.</returns>
    public Task<TProperty?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TProperty? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (input == null || !context.TryGetPayload(out TransformContext<TMessage>? transformContext) || !transformContext.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        var propertyTransformContext = new PropertyTransformContext<TMessage, TProperty>(transformContext, input);

        InitializeContext<TProperty> messageContext = _initializer.Create(propertyTransformContext);

        Task<InitializeContext<TProperty>> initTask = _initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken);
        if (initTask.IsCompletedSuccessfully)
            return Task.FromResult<TProperty?>(initTask.Result.Message);

        async Task<TProperty?> ConvertAsync()
        {
            InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);

            return result.Message;
        }

        return ConvertAsync();
    }
}
