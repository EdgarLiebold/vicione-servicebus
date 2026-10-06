using System;
using System.Threading;
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
        readonly ScopedServiceLifetime _lifetime;
        readonly IServiceScope _scope;
        readonly object _cacheGate = new();
        TFilter _filter = null!;
        bool _creatingFilter;
        int _creatingThread;

        public DependencyInjectionFilterScopeContext(TContext context, IServiceProvider serviceProvider)
        {
            Context = context;
            // Allocate the lifetime/task/cache infrastructure before acquiring an owned DI scope.
            _lifetime = new ScopedServiceLifetime(ReleaseScopeAsync);
            _scope = context.TryGetPayload(out IServiceProvider? provider)
                || (context.TryGetPayload(out ConsumeContext? consumeContext) && consumeContext.TryGetPayload(out provider))
                ? new NoopScope(provider)
                : serviceProvider.CreateScope();
        }

        public ValueTask DisposeAsync() => _lifetime.DisposeAsync();

        public IFilter<TContext> Filter => _lifetime.Resolve(ResolveFilter);

        (TFilter Value, bool Owned) ResolveFilter()
        {
            lock (_cacheGate)
            {
                while (_creatingFilter)
                {
                    if (_creatingThread == Environment.CurrentManagedThreadId)
                        throw new InvalidOperationException("A scoped filter cannot recursively resolve itself during construction.");
                    Monitor.Wait(_cacheGate);
                }
                if (_filter is not null)
                    return (_filter, false);
                _creatingFilter = true;
                _creatingThread = Environment.CurrentManagedThreadId;
            }

            try
            {
                // Neither the admission lock nor the cache lock surrounds provider or constructor code.
                var result = ScopedServiceLifetime.GetServiceOrCreate<TFilter>(_scope.ServiceProvider);
                lock (_cacheGate)
                    _filter = result.Value;
                return result;
            }
            finally
            {
                lock (_cacheGate)
                {
                    _creatingFilter = false;
                    _creatingThread = 0;
                    Monitor.PulseAll(_cacheGate);
                }
            }
        }

        ValueTask ReleaseScopeAsync()
        {
            if (_scope is IAsyncDisposable asyncDisposable)
                return asyncDisposable.DisposeAsync();
            _scope.Dispose();
            return default;
        }

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
