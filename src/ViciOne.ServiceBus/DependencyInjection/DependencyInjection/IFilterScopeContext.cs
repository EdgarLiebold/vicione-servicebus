// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface IFilterScopeContext<TContext> :
        IAsyncDisposable
        where TContext : class, PipeContext
    {
        IFilter<TContext> Filter { get; }
        TContext Context { get; }
    }
}
