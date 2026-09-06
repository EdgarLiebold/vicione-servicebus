using System.Text;
using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Instance-specific address for a service endpoint.</summary>
public class InstanceEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>Initializes a new instance.</summary>
    public InstanceEndpointDefinition()
    {
        var instanceId = NewId.Next();

        InstanceName = instanceId.ToString(ZBase32Formatter.LowerCase);
    }

    string InstanceName { get; }

    /// <summary>Gets a value indicating whether temporary.</summary>
    public bool IsTemporary => true;

    /// <summary>Gets the prefetch count.</summary>
    public int? PrefetchCount => default;

    /// <summary>Gets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit => default;

    /// <summary>Gets the configure consume topology.</summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>Gets endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The endpoint name.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        var sb = new StringBuilder(InstanceName.Length + 9);

        sb.Append("Instance");
        sb.Append('_');
        sb.Append(InstanceName);

        return formatter.SanitizeName(sb.ToString());
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="context">The context associated with the operation.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
