using System;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers message-sensitivity inspection and bounded diagnostic rendering services.</summary>
public static class MessageRedactionServiceCollectionExtensions
{
    /// <summary>Adds the default conservative diagnostic renderer without replacing application registrations.</summary>
    /// <param name="services">The dependency-injection service collection.</param>
    /// <returns>The supplied service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddViciOneMessageDiagnosticRedaction(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ViciOne.ServiceBus.Advanced.Serialization.IMessageSensitivityInspector,
            ViciOne.ServiceBus.Advanced.Serialization.MessageSensitivityInspector>();
        services.TryAddSingleton<ViciOne.ServiceBus.Advanced.Serialization.IMessageDiagnosticRedactor,
            ViciOne.ServiceBus.Advanced.Serialization.MessageDiagnosticRedactor>();
        return services;
    }
}
