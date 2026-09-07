namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for saga endpoint.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class SagaEndpointDefinition<TSaga> :
    SettingsEndpointDefinition<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public SagaEndpointDefinition(IEndpointSettings<IEndpointDefinition<TSaga>> settings)
        : base(settings)
    {
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.Saga<TSaga>();
    }
}
