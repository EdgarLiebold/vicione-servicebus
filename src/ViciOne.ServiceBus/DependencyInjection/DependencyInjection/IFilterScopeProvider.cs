// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    public interface IFilterScopeProvider<TContext> :
        IProbeSite
        where TContext : class, PipeContext
    {
        IFilterScopeContext<TContext> Create(TContext context);
    }
}
