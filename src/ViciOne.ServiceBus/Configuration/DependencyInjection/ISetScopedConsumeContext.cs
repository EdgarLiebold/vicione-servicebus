using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for set scoped consume context.
/// </summary>
public interface ISetScopedConsumeContext
{
    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="serviceProvider">The service provider value.</param>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context);
}
