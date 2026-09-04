using System.Text;
using ViciOne.ServiceBus.NewIdFormatters;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Instance-specific address for a service endpoint
/// </summary>
public class InstanceEndpointDefinition :
    IEndpointDefinition
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public InstanceEndpointDefinition()
    {
        var instanceId = NewId.Next();

        InstanceName = instanceId.ToString(ZBase32Formatter.LowerCase);
    }

    string InstanceName { get; }

    /// <summary>
    /// Gets the is temporary value.
    /// </summary>
    public bool IsTemporary => true;

    /// <summary>
    /// Gets the prefetch count value.
    /// </summary>
    public int? PrefetchCount => default;

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit => default;

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology => true;

    /// <summary>
    /// Gets endpoint name.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    public string GetEndpointName(IEndpointNameFormatter formatter)
    {
        var sb = new StringBuilder(InstanceName.Length + 9);

        sb.Append("Instance");
        sb.Append('_');
        sb.Append(InstanceName);

        return formatter.SanitizeName(sb.ToString());
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="context">The operation context.</param>
    public void Configure<T>(T configurator, IRegistrationContext? context)
        where T : IReceiveEndpointConfigurator
    {
    }
}
