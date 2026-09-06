namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for compensate activity endpoint.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
public class CompensateActivityEndpointDefinition<TActivity, TLog> :
    SettingsEndpointDefinition<ICompensateActivity<TLog>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public CompensateActivityEndpointDefinition(IEndpointSettings<IEndpointDefinition<ICompensateActivity<TLog>>> settings)
        : base(settings)
    {
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        return formatter.CompensateActivity<TActivity, TLog>();
    }
}
