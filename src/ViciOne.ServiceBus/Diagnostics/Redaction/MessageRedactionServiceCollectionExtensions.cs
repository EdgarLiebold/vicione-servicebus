using System;

using Microsoft.Extensions.DependencyInjection.Extensions;

#nullable enable

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>Registers ServiceBus diagnostic sensitivity and redaction services.</summary>
public static class MessageRedactionServiceCollectionExtensions
{
    /// <summary>Adds the conservative ServiceBus diagnostic redactor.</summary>
    public static IServiceCollection AddViciOneMessageDiagnosticRedaction(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ViciOne.ServiceBus.Diagnostics.IMessageSensitivityInspector,
            ViciOne.ServiceBus.Diagnostics.MessageSensitivityInspector>();
        services.TryAddSingleton<ViciOne.ServiceBus.Diagnostics.IMessageDiagnosticRedactor,
            ViciOne.ServiceBus.Diagnostics.MessageDiagnosticRedactor>();
        return services;
    }
}
