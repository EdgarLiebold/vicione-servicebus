using System;
using System.Text;
using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines a uniquely named temporary endpoint for one service instance.</summary>
public sealed class InstanceEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>Creates a temporary endpoint definition with a process-unique instance name.</summary>
    public InstanceEndpointDefinition()
    {
        var instanceId = NewId.Next();

        InstanceName = instanceId.ToString(ZBase32Formatter.LowerCase);
    }

    string InstanceName { get; }

    /// <summary>Gets a value indicating that the endpoint is temporary.</summary>
    public bool IsTemporary => true;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => default;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => default;

    /// <summary>Gets a value indicating that consume topology is configured.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Gets the sanitized queue name derived from this instance identifier.</summary>
    /// <param name="formatter">The formatter that sanitizes the generated instance name.</param>
    /// <returns>The transport-safe endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        var sb = new StringBuilder(InstanceName.Length + 9);

        sb.Append("Instance");
        sb.Append('_');
        sb.Append(InstanceName);

        return formatter.SanitizeName(sb.ToString());
    }

    /// <summary>Validates the endpoint configurator; this definition has no additional transport settings.</summary>
    /// <typeparam name="TConfigurator">The transport-specific receive-endpoint configurator type.</typeparam>
    /// <param name="configurator">The endpoint configuration to validate.</param>
    /// <param name="context">The optional registration context; this definition does not consume it.</param>
    public void Configure<TConfigurator>(TConfigurator configurator, IRegistrationContext? context)
        where TConfigurator : IReceiveEndpointConfigurator
    {
        ArgumentNullException.ThrowIfNull(configurator);
    }
}
