namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures exception selection and both branches of a rescue pipeline.</summary>
/// <typeparam name="TContext">The original pipeline context type.</typeparam>
/// <typeparam name="TRescue">The context type supplied to the rescue branch.</typeparam>
public interface IRescueConfigurator<TContext, TRescue> :
    IExceptionConfigurator,
    IPipeConfigurator<TRescue>
    where TContext : class, PipeContext
    where TRescue : class, TContext
{
    /// <summary>Gets the branch that runs on the original context before control returns to the rescue branch.</summary>
    IPipeConfigurator<TContext> ContextPipe { get; }
}
