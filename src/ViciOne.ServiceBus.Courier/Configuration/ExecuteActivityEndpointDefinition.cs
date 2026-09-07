namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for execute activity endpoint.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
internal sealed class ExecuteActivityEndpointDefinition<TActivity, TArguments> :
    SettingsEndpointDefinition<IExecuteActivity<TArguments>>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="settings">The settings that control the operation.</param>
    public ExecuteActivityEndpointDefinition(IEndpointSettings<IEndpointDefinition<IExecuteActivity<TArguments>>> settings)
        : base(settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
    }

    /// <summary>Formats endpoint name.</summary>
    /// <param name="formatter">The formatter.</param>
    /// <returns>The formatted endpoint name.</returns>
    protected override string FormatEndpointName(IEndpointNameFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        return formatter.ExecuteActivity<TActivity, TArguments>();
    }
}
