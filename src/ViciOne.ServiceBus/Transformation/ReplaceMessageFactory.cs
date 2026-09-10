using System;
using ViciOne.ServiceBus.Initializers;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Returns the current transform input as the initialized message instance.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
internal sealed class ReplaceMessageFactory<TMessage> :
    IMessageFactory<TMessage>
    where TMessage : class
{
    /// <summary>Uses the available transform input as the initialized message.</summary>
    /// <param name="context">The initialization context carrying the source transform.</param>
    /// <returns>An initialization context containing the existing message instance.</returns>
    public InitializeContext<TMessage> Create(InitializeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.TryGetPayload(out TransformContext<TMessage>? transformContext) && transformContext.HasInput)
            return context.CreateMessageContext(transformContext.Input);

        throw new InvalidOperationException("The original message context was not available.");
    }
}
