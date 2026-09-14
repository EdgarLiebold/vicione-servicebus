using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Installs and restores the ambient consume context for a dependency-injection scope.</summary>
public interface ISetScopedConsumeContext
{
    /// <summary>Installs a consume context for the lifetime of a message scope.</summary>
    /// <param name="serviceProvider">The active message scope.</param>
    /// <param name="context">The consume context exposed within that scope.</param>
    /// <returns>A handle that restores the previous ambient context when disposed.</returns>
    IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context);
}
