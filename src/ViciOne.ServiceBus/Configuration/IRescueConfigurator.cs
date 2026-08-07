// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
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
}
