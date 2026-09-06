namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for consumer endpoint.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerEndpointDefinition<TConsumer> :
    SettingsEndpointDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public ConsumerEndpointDefinition(IEndpointSettings<IEndpointDefinition<TConsumer>> settings)
        : base(settings)
    {
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Consumer<TConsumer>();
    }
}
