using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Adds a message handler to the consuming pipe builder
/// </summary>
/// <typeparam name="T">The message type</typeparam>
public class ObserverPipeSpecification<T> :
    IPipeSpecification<ConsumeContext<T>>
    where T : class
{
    readonly IObserver<ConsumeContext<T>> _observer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    public ObserverPipeSpecification(IObserver<ConsumeContext<T>> observer)
    {
        _observer = observer;
    }

    void IPipeSpecification<ConsumeContext<T>>.Apply(IPipeBuilder<ConsumeContext<T>> builder)
    {
        builder.AddFilter(new ObserverMessageFilter<T>(_observer));
    }

    IEnumerable<ValidationResult> ISpecification.Validate()
    {
        if (_observer == null)
            yield return this.Failure("Handler", "must not be null");
    }
}
