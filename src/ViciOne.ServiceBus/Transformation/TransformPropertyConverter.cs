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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="initializer">The initializer.</param>
    public TransformPropertyConverter(IMessageInitializer<TProperty> initializer)
    {
        _initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
    }

    /// <summary>Converts the supplied value.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="input">The input.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the converted value.</returns>
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
