using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for scoped consume context provider.
/// </summary>
public interface IScopedConsumeContextProvider
{
    /// <summary>
    /// Gets the has context value.
    /// </summary>
    bool HasContext { get; }
    /// <summary>
    /// Gets context.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    ConsumeContext GetContext();
    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IDisposable PushContext(ConsumeContext context);
}
