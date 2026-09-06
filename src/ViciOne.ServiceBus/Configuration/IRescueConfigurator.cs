namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures rescue.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
/// <typeparam name="TRescue">The rescue type.</typeparam>
public interface IRescueConfigurator<TContext, TRescue> :
    IExceptionConfigurator,
    IPipeConfigurator<TRescue>
    where TContext : class, PipeContext
    where TRescue : class, TContext
{
    /// <summary>Configure a filter on the context pipe, versus the rescue pipe.</summary>
    IPipeConfigurator<TContext> ContextPipe { get; }
}
