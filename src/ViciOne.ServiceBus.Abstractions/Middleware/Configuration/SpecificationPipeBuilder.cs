using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Builds specification pipe components.</summary>
    public class SpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly List<IFilter<TContext>> _filters;

        /// <summary>Initializes a new instance.</summary>
        public SpecificationPipeBuilder()
        {
            _filters = new List<IFilter<TContext>>(16);
        }

        /// <summary>Adds filter to the configuration.</summary>
        /// <param name="filter">The filter to add to the pipeline.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _filters.Add(filter);
        }

        /// <summary>Gets a value indicating whether delegated.</summary>
        public bool IsDelegated => false;
        /// <summary>Gets a value indicating whether implemented.</summary>
        public bool IsImplemented => false;

        /// <summary>Creates delegated builder.</summary>
        /// <returns>The created delegated builder.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>Creates implemented builder.</summary>
        /// <returns>The created implemented builder.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
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

        /// <summary>Builds the configured component.</summary>
        /// <param name="lastPipe">The last pipe.</param>
        /// <returns>The configured component.</returns>
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
