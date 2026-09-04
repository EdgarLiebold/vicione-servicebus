namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for rescue configurator.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
/// <typeparam name="TRescue">The t rescue type.</typeparam>
public interface IRescueConfigurator<TContext, TRescue> :
    IExceptionConfigurator,
    IPipeConfigurator<TRescue>
    where TContext : class, PipeContext
    where TRescue : class, TContext
{
    /// <summary>
    /// Configure a filter on the context pipe, versus the rescue pipe
    /// </summary>
    IPipeConfigurator<TContext> ContextPipe { get; }
}
