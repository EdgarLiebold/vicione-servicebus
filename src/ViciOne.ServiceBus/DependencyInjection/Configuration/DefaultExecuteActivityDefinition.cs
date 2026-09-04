namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a default execute activity definition implementation.
/// </summary>
/// <typeparam name="TActivity">The t activity type.</typeparam>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public class DefaultExecuteActivityDefinition<TActivity, TArguments> :
    ExecuteActivityDefinition<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}
