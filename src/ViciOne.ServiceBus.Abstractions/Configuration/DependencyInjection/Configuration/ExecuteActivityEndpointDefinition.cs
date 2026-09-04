namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides an execute activity endpoint definition implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class ExecuteActivityEndpointDefinition<TActivity, TArguments> :
    SettingsEndpointDefinition<IExecuteActivity<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    public ExecuteActivityEndpointDefinition(IEndpointSettings<IEndpointDefinition<IExecuteActivity<TArguments>>> settings)
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
        return formatter.ExecuteActivity<TActivity, TArguments>();
    }
}
