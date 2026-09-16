using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Used by Send/Publish filters to send within either a scoped endpoint/request client context or within the consume context
/// currently active.
/// </summary>
/// <typeparam name="TFilter">The filter type.</typeparam>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class FilterScopeProvider<TFilter, TContext> :
    IFilterScopeProvider<TContext>
    where TFilter : class, IFilter<TContext>
    where TContext : class, PipeContext
{
    readonly IServiceProvider _serviceProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serviceProvider">The service provider.</param>
    public FilterScopeProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>Creates the requested value.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The newly created instance.</returns>
    public IFilterScopeContext<TContext> Create(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new DependencyInjectionFilterScopeContext(context, _serviceProvider);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("filter", TypeCache<TFilter>.ShortName);
    }


    class DependencyInjectionFilterScopeContext :
        IFilterScopeContext<TContext>
    {
        readonly ServiceScopeLifetime _lifetime;
        readonly IServiceScope _scope;
        TFilter _filter = null!;

        public DependencyInjectionFilterScopeContext(TContext context, IServiceProvider serviceProvider)
        {
            Context = context;
            _scope = context.TryGetPayload(out IServiceProvider? provider)
                || (context.TryGetPayload(out ConsumeContext? consumeContext) && consumeContext.TryGetPayload(out provider))
                ? new NoopScope(provider)
                : serviceProvider.CreateScope();
            _lifetime = new ServiceScopeLifetime(_scope);
        }

        public ValueTask DisposeAsync() => _lifetime.DisposeAsync();

        public IFilter<TContext> Filter => _filter ??= ActivatorUtilities.GetServiceOrCreateInstance<TFilter>(_scope.ServiceProvider);

        public TContext Context { get; }


        class NoopScope :
            IServiceScope
        {
            public NoopScope(IServiceProvider serviceProvider)
            {
                ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            public void Dispose()
            {
            }

            public IServiceProvider ServiceProvider { get; }
        }
    }
}
