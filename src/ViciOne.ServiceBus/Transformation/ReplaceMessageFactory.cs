using System;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Creates replace message instances.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ReplaceMessageFactory<TMessage> :
    IMessageFactory<TMessage>
    where TMessage : class
{
    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        if (context.TryGetPayload(out TransformContext<TMessage>? transformContext) && transformContext.HasInput)
            return context.CreateMessageContext(transformContext.Input);

        throw new InvalidOperationException("The original message context was not available.");
    }
}
