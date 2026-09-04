namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a compensate activity endpoint definition implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class CompensateActivityEndpointDefinition<TActivity, TLog> :
    SettingsEndpointDefinition<ICompensateActivity<TLog>>
    where TActivity : class, ICompensateActivity<TLog>
    where TLog : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public CompensateActivityEndpointDefinition(IEndpointSettings<IEndpointDefinition<ICompensateActivity<TLog>>> settings)
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
        return formatter.CompensateActivity<TActivity, TLog>();
    }
}
