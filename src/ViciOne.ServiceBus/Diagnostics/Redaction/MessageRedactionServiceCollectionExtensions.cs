using System;

using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Microsoft.Extensions.DependencyInjection;
/// <summary>Registers ServiceBus diagnostic sensitivity and redaction services.</summary>
public static class MessageRedactionServiceCollectionExtensions
{
    /// <summary>Adds the conservative ServiceBus diagnostic redactor.</summary>
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
