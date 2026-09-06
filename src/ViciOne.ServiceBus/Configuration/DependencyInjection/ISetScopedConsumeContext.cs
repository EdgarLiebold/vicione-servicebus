using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Exposes state for set scoped consume operations.</summary>
public interface ISetScopedConsumeContext
{
    /// <summary>Pushes context.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context);
}
