namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides typed arguments and routing-slip state while an activity executes.</summary>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
public interface ExecuteActivityContext<out TArguments> :
    ExecuteContext<TArguments>
    where TArguments : class
{
}


/// <summary>Provides an activity instance together with its typed arguments and routing-slip context.</summary>
/// <typeparam name="TActivity">The activity implementation.</typeparam>
/// <typeparam name="TArguments">The execution-arguments contract.</typeparam>
public interface ExecuteActivityContext<out TActivity, out TArguments> :
    ExecuteActivityContext<TArguments>
    where TArguments : class
    where TActivity : class
{
    /// <summary>Gets the activity instance that performs execution.</summary>
    TActivity Activity { get; }
}
