namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a saga endpoint definition implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class SagaEndpointDefinition<TSaga> :
    SettingsEndpointDefinition<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public SagaEndpointDefinition(IEndpointSettings<IEndpointDefinition<TSaga>> settings)
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
        return formatter.Saga<TSaga>();
    }
}
