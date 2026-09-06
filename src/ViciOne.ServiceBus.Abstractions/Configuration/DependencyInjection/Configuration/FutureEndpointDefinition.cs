namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for future endpoint.</summary>
/// <typeparam name="TFuture">The future type.</typeparam>
public class FutureEndpointDefinition<TFuture> :
    SettingsEndpointDefinition<TFuture>
    where TFuture : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public FutureEndpointDefinition(IEndpointSettings<IEndpointDefinition<TFuture>> settings)
        : base(settings)
    {
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Message<TFuture>();
    }
}
