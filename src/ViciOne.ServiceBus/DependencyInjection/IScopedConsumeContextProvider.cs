using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides scoped consume context services.</summary>
public interface IScopedConsumeContextProvider
{
    /// <summary>Gets a value indicating whether this instance has context.</summary>
    bool HasContext { get; }
    /// <summary>Gets context.</summary>
    /// <returns>The current context, or <see langword="null" /> when no context is active.</returns>
    ConsumeContext? GetContext();
    /// <summary>Tries to get the current available context as one atomic snapshot.</summary>
    /// <param name="context">The current context when one is available; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when an available context was captured.</returns>
    bool TryGetContext([NotNullWhen(true)] out ConsumeContext? context)
    {
        context = GetContext();
        if (context != null && context is not UnavailableConsumeContext)
            return true;

        context = null;
        return false;
    }
    /// <summary>Pushes context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    IDisposable PushContext(ConsumeContext context);
}
