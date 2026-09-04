using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a pipe configurator implementation.
/// </summary>
public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>
    /// Provides a specification pipe builder implementation.
    /// </summary>
    public class SpecificationPipeBuilder :
        ISpecificationPipeBuilder<TContext>
    {
        readonly List<IFilter<TContext>> _filters;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        public SpecificationPipeBuilder()
        {
            _filters = new List<IFilter<TContext>>(16);
        }

        /// <summary>
        /// Adds filter to the configuration.
        /// </summary>
        /// <param name="filter">The filter value.</param>
        public void AddFilter(IFilter<TContext> filter)
        {
            _filters.Add(filter);
        }

        /// <summary>
        /// Gets the is delegated value.
        /// </summary>
        public bool IsDelegated => false;
        /// <summary>
        /// Gets the is implemented value.
        /// </summary>
        public bool IsImplemented => false;

        /// <summary>
        /// Creates delegated builder.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, IsImplemented, true);
        }

        /// <summary>
        /// Creates implemented builder.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder()
        {
            return new ChildSpecificationPipeBuilder(this, true, IsDelegated);
        }

        /// <summary>
        /// Performs the build operation.
        /// </summary>
        /// <returns>The result of the operation.</returns>
        public IPipe<TContext> Build()
        {
            if (_filters.Count == 0)
                return Cache.EmptyPipe;

            IPipe<TContext> current = new LastPipe(_filters[_filters.Count - 1]);

            for (var i = _filters.Count - 2; i >= 0; i--)
                current = new FilterPipe(_filters[i], current);

            return current;
        }

        /// <summary>
        /// Performs the build operation.
        /// </summary>
        /// <param name="lastPipe">The last pipe value.</param>
        /// <returns>The result of the operation.</returns>
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
