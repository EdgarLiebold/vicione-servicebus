using System;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Provides a replace message factory implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ReplaceMessageFactory<TMessage> :
    IMessageFactory<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Performs the create operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        if (context.TryGetPayload(out TransformContext<TMessage>? transformContext) && transformContext.HasInput)
            return context.CreateMessageContext(transformContext.Input);

        throw new InvalidOperationException("The original message context was not available.");
    }
}
