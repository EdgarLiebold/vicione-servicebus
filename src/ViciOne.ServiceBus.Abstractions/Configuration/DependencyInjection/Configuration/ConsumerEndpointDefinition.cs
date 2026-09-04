namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer endpoint definition implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ConsumerEndpointDefinition<TConsumer> :
    SettingsEndpointDefinition<TConsumer>
    where TConsumer : class, IConsumer
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public ConsumerEndpointDefinition(IEndpointSettings<IEndpointDefinition<TConsumer>> settings)
        : base(settings)
    {
    }

    /// <summary>
    /// Performs the format endpoint name operation.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    /// <returns>The result of the operation.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Consumer<TConsumer>();
    }
}
