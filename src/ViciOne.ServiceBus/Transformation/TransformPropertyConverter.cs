using System.Threading.Tasks;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Provides a transform property converter implementation.
/// </summary>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class TransformPropertyConverter<TProperty> :
    IPropertyConverter<TProperty, TProperty>
    where TProperty : class
{
    readonly IMessageInitializer<TProperty> _initializer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="initializer">The initializer value.</param>
    public TransformPropertyConverter(IMessageInitializer<TProperty> initializer)
    {
        _initializer = initializer;
    }

    /// <summary>
    /// Performs the convert operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="input">The input value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<TProperty?> ConvertAsync<TMessage>(InitializeContext<TMessage> context, TProperty? input, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (input == null || !context.TryGetPayload(out TransformContext<TMessage>? transformContext) || !transformContext.HasInput)
            return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

        var propertyTransformContext = new PropertyTransformContext<TMessage, TProperty>(transformContext, input);

        InitializeContext<TProperty> messageContext = _initializer.Create(propertyTransformContext);

        Task<InitializeContext<TProperty>> initTask = _initializer.InitializeAsync(messageContext, input, cancellationToken: cancellationToken);
        if (initTask.IsCompleted)
            return Task.FromResult<TProperty?>(initTask.Result.Message);

        async Task<TProperty?> ConvertAsync()
        {
            InitializeContext<TProperty> result = await initTask.ConfigureAwait(false);

            return result.Message;
        }

        return ConvertAsync();
    }
}
