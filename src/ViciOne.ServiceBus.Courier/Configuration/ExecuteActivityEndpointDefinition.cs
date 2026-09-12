namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies explicit endpoint settings and Courier naming to an activity execution endpoint.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityEndpointDefinition<TActivity, TArguments> :
    SettingsEndpointDefinition<IExecuteActivity<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Creates an execution endpoint definition from registration settings.</summary>
    /// <param name="settings">The endpoint settings to apply.</param>
    public ExecuteActivityEndpointDefinition(IEndpointSettings<IEndpointDefinition<IExecuteActivity<TArguments>>> settings)
        : base(settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
    }

    /// <summary>Derives the execution endpoint name from the activity and argument contracts.</summary>
    /// <param name="formatter">The application endpoint-name formatter.</param>
    /// <returns>The formatted execution endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return formatter.ExecuteActivity<TActivity, TArguments>();
    }
}
