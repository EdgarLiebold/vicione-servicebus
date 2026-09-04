namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a future endpoint definition implementation.
/// </summary>
/// <typeparam name="TFuture">The t future type.</typeparam>
public class FutureEndpointDefinition<TFuture> :
    SettingsEndpointDefinition<TFuture>
    where TFuture : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public FutureEndpointDefinition(IEndpointSettings<IEndpointDefinition<TFuture>> settings)
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
        return formatter.Message<TFuture>();
    }
}
