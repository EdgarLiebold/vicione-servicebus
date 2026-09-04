namespace ViciOne.ServiceBus.DependencyInjection;

public interface IFilterScopeProvider<TContext> :
    IProbeSite
    where TContext : class, PipeContext
{
    IFilterScopeContext<TContext> Create(TContext context);
}
