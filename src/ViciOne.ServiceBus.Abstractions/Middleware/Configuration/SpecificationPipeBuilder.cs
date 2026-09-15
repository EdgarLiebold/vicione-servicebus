using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Accumulates specification filters and composes them in registration order.</summary>
    public class SpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly List<IFilter<TContext>> _filters;

        /// <summary>Creates an empty builder without delegated or implemented application markers.</summary>
        public SpecificationPipeBuilder()
        {
            _filters = new List<IFilter<TContext>>(16);
        }

        /// <summary>Appends a filter to the configured execution order.</summary>
        /// <param name="filter">The filter included in subsequent pipeline builds.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _filters.Add(filter);
        }

        /// <summary>Gets false because this builder does not suppress implemented-message-type specifications.</summary>
        public bool IsDelegated => false;
        /// <summary>Gets false because this builder does not suppress base message specifications.</summary>
        public bool IsImplemented => false;

        /// <summary>Creates a delegated wrapper while preserving the implemented marker.</summary>
        /// <returns>A wrapper that appends filters here and suppresses implemented-message-type specifications.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>Creates an implemented wrapper while preserving the delegated marker.</summary>
        /// <returns>A wrapper that appends filters here and suppresses base message specifications.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
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

        /// <summary>Composes the registered filters in execution order before the supplied continuation.</summary>
        /// <param name="lastPipe">The pipeline invoked after the registered filters.</param>
        /// <returns>The composed pipeline, or the supplied continuation unchanged when no filters are registered.</returns>
        public IPipe<TContext> Build(IPipe<TContext> lastPipe)
        {
            if (_filters.Count == 0)
                return lastPipe;

            IPipe<TContext> current = lastPipe;

            for (var i = _filters.Count - 1; i >= 0; i--)
                current = new FilterPipe(_filters[i], current);

            return current;
        }
    }
}
