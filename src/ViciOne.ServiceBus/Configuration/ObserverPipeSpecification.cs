using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds a consume observer as the terminal filter for a message pipeline.</summary>
/// <typeparam name="TMessage">The observed message contract.</typeparam>
public sealed class ObserverPipeSpecification<TMessage> :
    IPipeSpecification<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly IObserver<ConsumeContext<TMessage>> _observer;

    /// <summary>Creates a terminal pipeline specification for a typed consume observer.</summary>
    /// <param name="observer">The observer notified by the terminal filter.</param>
    public ObserverPipeSpecification(IObserver<ConsumeContext<TMessage>> observer)
    {
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
    }

    void IPipeSpecification<ConsumeContext<TMessage>>.Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new ObserverMessageFilter<TMessage>(_observer));
    }

    IEnumerable<ValidationResult> ISpecification.Validate() => [];
}
