// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    public interface IBuildPipeConfigurator<TContext> :
        IPipeConfigurator<TContext>,
        ISpecification
        where TContext : class, PipeContext
    {
        /// <summary>
        /// Builds the pipe, applying any initial specifications to the front of the pipe
        /// </summary>
        /// <returns></returns>
        IPipe<TContext> Build();
    }
}
