using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Accumulates filters and composes them in registration order.</summary>
    public class PipeBuilder :
        IPipeBuilder<TContext>
    {
        readonly List<IFilter<TContext>> _filters;

        /// <summary>Creates an empty builder with the requested initial filter capacity.</summary>
        /// <param name="capacity">The initial capacity of the filter collection.</param>
        public PipeBuilder(int capacity = 16)
        {
            _filters = new List<IFilter<TContext>>(capacity);
        }

        /// <summary>Creates a builder by copying the supplied filters in execution order.</summary>
        /// <param name="filters">The initial filter collection.</param>
        public PipeBuilder(params IFilter<TContext>[] filters)
        {
            _filters = new List<IFilter<TContext>>(filters);
        }

        /// <summary>Appends a filter to the configured execution order.</summary>
        /// <param name="filter">The filter included in subsequent pipeline builds.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _filters.Add(filter);
        }

        /// <summary>Composes the registered filters in execution order with an empty terminating continuation.</summary>
        /// <returns>The composed pipeline, or the cached empty pipeline when no filters are registered.</returns>
        public IPipe<TContext> Build()
        {
            if (_filters.Count == 0)
                return Cache.EmptyPipe;

            IPipe<TContext> current = new LastPipe(_filters[_filters.Count - 1]);

            for (var i = _filters.Count - 2; i >= 0; i--)
                current = new FilterPipe(_filters[i], current);

            return current;
        }
    }


    internal static class Cache
    {
        internal static readonly IPipe<TContext> EmptyPipe = new EmptyPipe();
        internal static readonly IPipe<TContext> LastPipe = new Last();
    }


    /// <summary>Terminates a pipeline with a completed task and no probe entries.</summary>
    public class EmptyPipe :
        IPipe<TContext>
    {
        [DebuggerNonUserCode]
        Task IPipe<TContext>.SendAsync(TContext context)
        {
            return Task.CompletedTask;
        }

        void IProbeSite.Probe(ProbeContext context)
        {
        }
    }


    /// <summary>Invokes one filter with an explicit continuation pipeline.</summary>
    public class FilterPipe :
        IPipe<TContext>
    {
        readonly IFilter<TContext> _filter;
        readonly IPipe<TContext> _next;

        /// <summary>Associates a filter with the continuation passed to it.</summary>
        /// <param name="filter">The filter invoked for each context.</param>
        /// <param name="next">The continuation passed to the filter.</param>
        public FilterPipe(IFilter<TContext> filter, IPipe<TContext> next)
        {
            _filter = filter;
            _next = next;
        }

        /// <summary>Writes probe entries for the filter and its continuation.</summary>
        /// <param name="context">The probe receiving both pipeline stages' entries.</param>
        public void Probe(ProbeContext context)
        {
            _filter.Probe(context);
            _next.Probe(context);
        }

        /// <summary>Passes the context and continuation to the filter.</summary>
        /// <param name="context">The context handled by the filter.</param>
        /// <returns>The task returned by the filter.</returns>
        [DebuggerStepThrough]
        public Task SendAsync(TContext context)
        {
            return _filter.SendAsync(context, _next);
        }
    }


    /// <summary>Invokes the last registered filter with the cached terminating continuation.</summary>
    public class LastPipe :
        IPipe<TContext>
    {
        readonly IFilter<TContext> _filter;

        /// <summary>Associates the final filter with the terminating continuation.</summary>
        /// <param name="filter">The filter invoked before the pipeline terminates.</param>
        public LastPipe(IFilter<TContext> filter)
        {
            _filter = filter;
        }

        /// <summary>Writes probe entries for the final filter.</summary>
        /// <param name="context">The probe receiving the filter's entries.</param>
        public void Probe(ProbeContext context)
        {
            _filter.Probe(context);
        }

        /// <summary>Passes the context and terminating continuation to the final filter.</summary>
        /// <param name="context">The context handled by the filter.</param>
        /// <returns>The task returned by the filter.</returns>
        [DebuggerStepThrough]
        public Task SendAsync(TContext context)
        {
            return _filter.SendAsync(context, Cache.LastPipe);
        }
    }


    class Last :
        IPipe<TContext>
    {
        public void Probe(ProbeContext context)
        {
        }

        public Task SendAsync(TContext context)
        {
            return Task.CompletedTask;
        }
    }
}
