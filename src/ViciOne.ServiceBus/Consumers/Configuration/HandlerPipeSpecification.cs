using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a message handler to the consuming pipe builder.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class HandlerPipeSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly MessageHandler<T> _handler;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="handler">The handler.</param>
    public HandlerPipeSpecification(MessageHandler<T> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    void IPipeSpecification<ConsumeContext<T>>.Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new HandlerMessageFilter<T>(_handler));
    }

    IEnumerable<ValidationResult> ISpecification.Validate() => [];
}
