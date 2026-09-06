using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped consume context services.</summary>
public interface IScopedConsumeContextProvider
{
    /// <summary>Gets a value indicating whether this instance has context.</summary>
    bool HasContext { get; }
    /// <summary>Gets context.</summary>
    /// <returns>The context.</returns>
    ConsumeContext GetContext();
    /// <summary>Pushes context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    IDisposable PushContext(ConsumeContext context);
}
