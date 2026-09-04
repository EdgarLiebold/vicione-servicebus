using System;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface IFilterScopeContext<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    IFilter<TContext> Filter { get; }
    TContext Context { get; }
}
