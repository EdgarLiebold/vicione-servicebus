namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a default activity definition implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
/// <typeparam name="TLog">The t log type.</typeparam>
public class DefaultActivityDefinition<TActivity, TArguments, TLog> :
    ActivityDefinition<TActivity, TArguments, TLog>
    where TActivity : class, IActivity<TArguments, TLog>
    where TArguments : class
    where TLog : class
{
}
