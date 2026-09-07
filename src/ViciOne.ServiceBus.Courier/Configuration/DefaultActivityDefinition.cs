namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for default activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
/// <typeparam name="TLog">The log type.</typeparam>
internal sealed class DefaultActivityDefinition<TActivity, TArguments, TLog> :
    ActivityDefinition<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
}
