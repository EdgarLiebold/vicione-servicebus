namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines configuration for default execute activity.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public class DefaultExecuteActivityDefinition<TActivity, TArguments> :
    ExecuteActivityDefinition<TActivity, TArguments>
    where TActivity : class, IExecuteActivity<TArguments>
    where TArguments : class
{
}
