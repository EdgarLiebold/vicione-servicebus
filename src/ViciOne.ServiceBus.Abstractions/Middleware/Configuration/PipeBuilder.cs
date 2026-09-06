using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Builds pipe components.</summary>
    public class PipeBuilder :
        IPipeBuilder<TContext>
    {
        readonly List<IFilter<TContext>> _filters;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="capacity">The capacity.</param>
        public PipeBuilder(int capacity = 16)
        {
            _filters = new List<IFilter<TContext>>(capacity);
        }

        /// <summary>Initializes a new instance.</summary>
        /// <param name="filters">The filters.</param>
        public PipeBuilder(params IFilter<TContext>[] filters)
        {
            _filters = new List<IFilter<TContext>>(filters);
        }

        /// <summary>Adds filter to the configuration.</summary>
        /// <param name="filter">The filter to add to the pipeline.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _filters.Add(filter);
        }

        /// <summary>Builds the configured component.</summary>
        /// <returns>The configured component.</returns>
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


    /// <summary>Executes the pipeline for empty.</summary>
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


    /// <summary>Executes the pipeline for filter.</summary>
    public class FilterPipe :
        IPipe<TContext>
    {
        readonly IFilter<TContext> _filter;
        readonly IPipe<TContext> _next;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="filter">The filter to add to the pipeline.</param>
        /// <param name="next">The next pipeline stage to invoke.</param>
        public FilterPipe(IFilter<TContext> filter, IPipe<TContext> next)
        {
            _filter = filter;
            _next = next;
        }

        /// <summary>Writes diagnostic information to the probe context.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public void Probe(ProbeContext context)
        {
            _filter.Probe(context);
            _next.Probe(context);
        }

        /// <summary>Sends a message to the configured destination.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        [DebuggerStepThrough]
        public Task SendAsync(TContext context)
        {
            return _filter.SendAsync(context, _next);
        }
    }


    /// <summary>The last pipe in a pipeline is always an end pipe that does nothing and returns synchronously.</summary>
    public class LastPipe :
        IPipe<TContext>
    {
        readonly IFilter<TContext> _filter;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="filter">The filter to add to the pipeline.</param>
        public LastPipe(IFilter<TContext> filter)
        {
            _filter = filter;
        }

        /// <summary>Writes diagnostic information to the probe context.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public void Probe(ProbeContext context)
        {
            _filter.Probe(context);
        }

        /// <summary>Sends a message to the configured destination.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
