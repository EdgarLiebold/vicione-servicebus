namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for execute activity operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface ExecuteActivityContext<out TArguments> :
    ExecuteContext<TArguments>
    where TArguments : class
{
}


/// <summary>An activity and execution context combined into a single container from the factory.</summary>
/// <typeparam name="TActivity">The activity type.</typeparam>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface ExecuteActivityContext<out TActivity, out TArguments> :
    ExecuteActivityContext<TArguments>
    where TArguments : class
    where TActivity : class
{
    /// <summary>The activity that was created/used for this execution.</summary>
    TActivity Activity { get; }
}
