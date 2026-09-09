namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines a receive endpoint whose default name is derived from a consumer type.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerEndpointDefinition<TConsumer> :
    SettingsEndpointDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Creates a consumer endpoint definition backed by the supplied settings.</summary>
    /// <param name="settings">The transport-independent endpoint settings.</param>
    public ConsumerEndpointDefinition(IEndpointSettings<IEndpointDefinition<TConsumer>> settings)
        : base(settings)
    {
    }

    /// <summary>Derives the endpoint name from the consumer type.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The consumer endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Consumer<TConsumer>();
    }
}
