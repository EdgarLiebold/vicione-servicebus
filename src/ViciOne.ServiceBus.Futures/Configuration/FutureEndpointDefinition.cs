namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines a receive endpoint whose default name is derived from a future type.</summary>
/// <typeparam name="TFuture">The future implementation type.</typeparam>
public class FutureEndpointDefinition<TFuture> :
    SettingsEndpointDefinition<TFuture>
    where TFuture : class
{
    /// <summary>Creates a future endpoint definition backed by the supplied settings.</summary>
    /// <param name="settings">The transport-independent endpoint settings.</param>
    public FutureEndpointDefinition(IEndpointSettings<IEndpointDefinition<TFuture>> settings)
        : base(settings)
    {
    }

    /// <summary>Derives the endpoint name from the future type.</summary>
    /// <param name="formatter">The endpoint naming convention.</param>
    /// <returns>The future endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Message<TFuture>();
    }
}
